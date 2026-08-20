using System.Runtime.InteropServices;
using System.Text;
using _3dEngine.Inputs.Interfaces;

namespace _3dEngine.Inputs.Implementations;

/// <summary>
/// Провайдер ввода через evdev (/dev/input/event*).
/// Работает одинаково на X11 и Wayland, не зависит от compositor.
/// Требует прав на чтение устройств ввода (группа input или root).
/// </summary>
internal class EvdevInputProvider : IInputProvider
{
    // --- P/Invoke (libc) ---
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern nint read(int fd, byte[] buf, nuint count);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, uint request, byte[] argp);

    [DllImport("libc", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);

    [DllImport("libc", SetLastError = true)]
    private static extern int poll([In, Out] PollFd[] fds, nuint nfds, int timeout);

    // --- Константы ---
    private const int O_RDONLY = 0;
    private const int O_NONBLOCK = 0x800;      // x86_64 / aarch64
    private const int F_GETFL = 3;
    private const int F_SETFL = 4;
    private const int POLLIN = 0x1;
    private const int POLLERR = 0x8;
    private const int EV_KEY = 0x01;
    private const int EV_SYN = 0x00;
    private const int KEY_MAX = 768;

    // ioctl EVIOCGBIT(ev, len) = _IOC(READ, 'E', 0x20+ev, len) = (2<<30) | (0x45<<8) | (0x20+ev) | (len<<16)
    private const uint EVIOCGBIT_TYPES = 0x80084520u;   // EVIOCGBIT(0, 8)  — маска типов событий
    private const uint EVIOCGBIT_KEYS = 0x80604521u;    // EVIOCGBIT(1, 96) — маска клавиш EV_KEY
    private const uint EVIOCGNAME_256 = 0x81004506u;    // EVIOCGNAME(256)  — имя устройства

    private const int EventSize = 24;                   // sizeof(struct input_event)
    private const int EventBufferSize = 24 * 64;        // до 64 событий за одно чтение

    // --- Поля ---
    private int _fd = -1;
    private string _deviceName = string.Empty;
    private byte[] _keyState = new byte[KEY_MAX / 8];   // 96 байт, битовая маска нажатых клавиш
    private readonly byte[] _readBuffer = new byte[EventBufferSize];
    private readonly PollFd[] _pollFds = new PollFd[1];
    private readonly Dictionary<int, ConsoleKey> _evdevToConsoleKey = new();
    private readonly Dictionary<ConsoleKey, int> _consoleToEvdevKey = new();
    private int _lShiftCode, _rShiftCode, _lCtrlCode, _rCtrlCode, _lAltCode, _rAltCode;

    public bool IsAvailable { get; private set; }
    public string? InitializationError { get; private set; }

    public EvdevInputProvider()
    {
        // Коды модификаторов (стандартные evdev KEY_* коды)
        _lShiftCode = 42;   // KEY_LEFTSHIFT
        _rShiftCode = 54;   // KEY_RIGHTSHIFT
        _lCtrlCode = 29;    // KEY_LEFTCTRL
        _rCtrlCode = 97;    // KEY_RIGHTCTRL
        _lAltCode = 56;     // KEY_LEFTALT
        _rAltCode = 100;    // KEY_RIGHTALT

        BuildKeyMaps();

        try
        {
            // Ищем клавиатуру, перебирая /sys/class/input/event*
            foreach (string sysfsDir in Directory.GetDirectories("/sys/class/input"))
            {
                string entryName = Path.GetFileName(sysfsDir);
                if (!entryName.StartsWith("event", StringComparison.Ordinal)) continue;

                string devicePath = "/dev/input/" + entryName;
                int fd = open(devicePath, O_RDONLY | O_NONBLOCK);
                if (fd < 0) continue;   // нет прав или устройство недоступно

                if (IsKeyboardDevice(fd, out string? deviceName))
                {
                    _fd = fd;
                    _deviceName = deviceName ?? entryName;
                    IsAvailable = true;

                    // Гарантируем неблокирующий режим через fcntl
                    int flags = fcntl(_fd, F_GETFL, 0);
                    if (flags >= 0) fcntl(_fd, F_SETFL, flags | O_NONBLOCK);

                    return; // fd остаётся открытым
                }

                close(fd);
            }
        }
        catch (DllNotFoundException)
        {
            InitializationError = "WARNING: 'libc' is missing. evdev input unavailable.";
        }
        catch (Exception ex)
        {
            InitializationError = $"WARNING: evdev initialization failed: {ex.Message}";
        }

        if (!IsAvailable)
        {
            InitializationError ??=
                "WARNING: Keyboard not found via evdev (/dev/input/event*).\n" +
                "-> Fix this by adding user to the 'input' group: sudo usermod -aG input $USER";
        }
    }

    public void Update()
    {
        if (_fd < 0) return;

        // Неблокирующий опрос: читаем все накопившиеся события
        while (true)
        {
            _pollFds[0] = new PollFd { fd = _fd, events = POLLIN };
            int pollResult = poll(_pollFds, (nuint)_pollFds.Length, 0);
            if (pollResult <= 0) break;                                  // нет данных
            if ((_pollFds[0].revents & POLLIN) == 0) break;              // POLLERR/POLLHUP — устройство отключено

            nint bytesRead = read(_fd, _readBuffer, (nuint)_readBuffer.Length);
            if (bytesRead <= 0) break;                                   // EAGAIN или ошибка чтения

            int eventCount = (int)(bytesRead / EventSize);
            for (int i = 0; i < eventCount; i++)
            {
                InputEvent evt = MemoryMarshal.Read<InputEvent>(_readBuffer.AsSpan(i * EventSize, EventSize));
                if (evt.Type != EV_KEY) continue;
                if (evt.Code >= KEY_MAX) continue;

                if (evt.Value == 1) // нажатие
                {
                    _keyState[evt.Code / 8] |= (byte)(1 << (evt.Code % 8));
                }
                else if (evt.Value == 0) // отпускание
                {
                    _keyState[evt.Code / 8] &= (byte)~(1 << (evt.Code % 8));
                }
                // value == 2 (autorepeat) — бит уже установлен, игнорируем
            }
        }
    }

    public bool IsGetKey(ConsoleKey key)
    {
        if (!_consoleToEvdevKey.TryGetValue(key, out int code)) return false;
        return IsPressed(code);
    }

    public bool IsGetKey(int virtualKey)
    {
        return IsGetKey((ConsoleKey)virtualKey);
    }

    public bool IsShift => IsPressed(_lShiftCode) || IsPressed(_rShiftCode);
    public bool IsCtrl => IsPressed(_lCtrlCode) || IsPressed(_rCtrlCode);
    public bool IsAlt => IsPressed(_lAltCode) || IsPressed(_rAltCode);

    private bool IsPressed(int code)
    {
        if (code < 0 || code >= KEY_MAX) return false;
        return (_keyState[code / 8] & (1 << (code % 8))) != 0;
    }

    public void Dispose()
    {
        if (_fd >= 0)
        {
            close(_fd);
            _fd = -1;
        }
        GC.SuppressFinalize(this);
    }

    // --- Служебное ---

    private bool IsKeyboardDevice(int fd, out string? deviceName)
    {
        deviceName = null;

        // Проверяем, что устройство генерирует события EV_KEY
        byte[] types = new byte[8];
        if (ioctl(fd, EVIOCGBIT_TYPES, types) < 0) return false;
        if ((types[EV_KEY / 8] & (1 << (EV_KEY % 8))) == 0) return false;

        // Проверяем наличие буквенных клавиш (KEY_A=30, KEY_W=17, KEY_S=31, KEY_D=32)
        byte[] keys = new byte[KEY_MAX / 8];
        if (ioctl(fd, EVIOCGBIT_KEYS, keys) < 0) return false;
        if (!IsKeySet(keys, 30) || !IsKeySet(keys, 17) || !IsKeySet(keys, 31) || !IsKeySet(keys, 32)) return false;

        // ...и всех четырёх стрелок (KEY_UP=103, KEY_LEFT=105, KEY_RIGHT=106, KEY_DOWN=108)
        if (!IsKeySet(keys, 103) || !IsKeySet(keys, 105) || !IsKeySet(keys, 106) || !IsKeySet(keys, 108)) return false;

        // Имя устройства (опционально, для диагностики)
        byte[] nameBuffer = new byte[256];
        if (ioctl(fd, EVIOCGNAME_256, nameBuffer) >= 0)
        {
            int nulIndex = Array.IndexOf(nameBuffer, (byte)0);
            int length = nulIndex < 0 ? nameBuffer.Length : nulIndex;
            deviceName = Encoding.ASCII.GetString(nameBuffer, 0, length);
        }

        return true;
    }

    private static bool IsKeySet(byte[] mask, int code)
    {
        return code >= 0 && code < KEY_MAX && (mask[code / 8] & (1 << (code % 8))) != 0;
    }

    private void BuildKeyMaps()
    {
        foreach (var pair in EvdevKeyToConsoleKeyMap)
        {
            _evdevToConsoleKey[pair.Key] = pair.Value;
            // TryAdd — не перезаписываем первичные клавиши (например, Enter=28 важнее KPENTER=96)
            _consoleToEvdevKey.TryAdd(pair.Value, pair.Key);
        }
    }

    // --- Структуры (Linux uapi) ---

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct InputEvent
    {
        public long TimeSec;    // tv_sec
        public long TimeUsec;   // tv_usec
        public ushort Type;     // EV_KEY=1, EV_SYN=0
        public ushort Code;     // KEY_* код
        public int Value;       // 0=release, 1=press, 2=autorepeat
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    #region КАРТА СОПОСТАВЛЕНИЯ EVDEV KEYCODE -> CONSOLEKEY

        private static readonly Dictionary<int, ConsoleKey> EvdevKeyToConsoleKeyMap = new()
        {
            // Системные и управляющие клавиши
            { 1, ConsoleKey.Escape },       // KEY_ESC
            { 15, ConsoleKey.Tab },         // KEY_TAB
            { 28, ConsoleKey.Enter },       // KEY_ENTER
            { 14, ConsoleKey.Backspace },   // KEY_BACKSPACE
            { 111, ConsoleKey.Delete },     // KEY_DELETE
            { 102, ConsoleKey.Home },       // KEY_HOME
            { 107, ConsoleKey.End },        // KEY_END
            { 104, ConsoleKey.PageUp },     // KEY_PAGEUP
            { 109, ConsoleKey.PageDown },   // KEY_PAGEDOWN
            { 110, ConsoleKey.Insert },     // KEY_INSERT
            { 57, ConsoleKey.Spacebar },    // KEY_SPACE

            // Стрелки
            { 103, ConsoleKey.UpArrow },    // KEY_UP
            { 105, ConsoleKey.LeftArrow },  // KEY_LEFT
            { 106, ConsoleKey.RightArrow }, // KEY_RIGHT
            { 108, ConsoleKey.DownArrow },  // KEY_DOWN

            // Цифры (основная клавиатура)
            { 11, ConsoleKey.D0 },          // KEY_0
            { 2, ConsoleKey.D1 },           // KEY_1
            { 3, ConsoleKey.D2 },           // KEY_2
            { 4, ConsoleKey.D3 },           // KEY_3
            { 5, ConsoleKey.D4 },           // KEY_4
            { 6, ConsoleKey.D5 },           // KEY_5
            { 7, ConsoleKey.D6 },           // KEY_6
            { 8, ConsoleKey.D7 },           // KEY_7
            { 9, ConsoleKey.D8 },           // KEY_8
            { 10, ConsoleKey.D9 },          // KEY_9

            // Алфавит (A-Z)
            { 30, ConsoleKey.A },           // KEY_A
            { 48, ConsoleKey.B },           // KEY_B
            { 46, ConsoleKey.C },           // KEY_C
            { 32, ConsoleKey.D },           // KEY_D
            { 18, ConsoleKey.E },           // KEY_E
            { 33, ConsoleKey.F },           // KEY_F
            { 34, ConsoleKey.G },           // KEY_G
            { 35, ConsoleKey.H },           // KEY_H
            { 23, ConsoleKey.I },           // KEY_I
            { 36, ConsoleKey.J },           // KEY_J
            { 37, ConsoleKey.K },           // KEY_K
            { 38, ConsoleKey.L },           // KEY_L
            { 50, ConsoleKey.M },           // KEY_M
            { 49, ConsoleKey.N },           // KEY_N
            { 24, ConsoleKey.O },           // KEY_O
            { 25, ConsoleKey.P },           // KEY_P
            { 16, ConsoleKey.Q },           // KEY_Q
            { 19, ConsoleKey.R },           // KEY_R
            { 31, ConsoleKey.S },           // KEY_S
            { 20, ConsoleKey.T },           // KEY_T
            { 22, ConsoleKey.U },           // KEY_U
            { 47, ConsoleKey.V },           // KEY_V
            { 17, ConsoleKey.W },           // KEY_W
            { 45, ConsoleKey.X },           // KEY_X
            { 21, ConsoleKey.Y },           // KEY_Y
            { 44, ConsoleKey.Z },           // KEY_Z

            // Знаки препинания (OEM-клавиши)
            { 12, ConsoleKey.OemMinus },    // KEY_MINUS
            { 13, ConsoleKey.OemPlus },     // KEY_EQUAL
            { 26, ConsoleKey.Oem4 },        // KEY_LEFTBRACE
            { 27, ConsoleKey.Oem6 },        // KEY_RIGHTBRACE
            { 39, ConsoleKey.Oem1 },        // KEY_SEMICOLON
            { 40, ConsoleKey.Oem7 },        // KEY_APOSTROPHE
            { 41, ConsoleKey.Oem3 },        // KEY_GRAVE
            { 43, ConsoleKey.Oem5 },        // KEY_BACKSLASH
            { 51, ConsoleKey.OemComma },    // KEY_COMMA
            { 52, ConsoleKey.OemPeriod },   // KEY_DOT
            { 53, ConsoleKey.Oem2 },        // KEY_SLASH

            // Функциональные клавиши (F1-F12)
            { 59, ConsoleKey.F1 },          // KEY_F1
            { 60, ConsoleKey.F2 },          // KEY_F2
            { 61, ConsoleKey.F3 },          // KEY_F3
            { 62, ConsoleKey.F4 },          // KEY_F4
            { 63, ConsoleKey.F5 },          // KEY_F5
            { 64, ConsoleKey.F6 },          // KEY_F6
            { 65, ConsoleKey.F7 },          // KEY_F7
            { 66, ConsoleKey.F8 },          // KEY_F8
            { 67, ConsoleKey.F9 },          // KEY_F9
            { 68, ConsoleKey.F10 },         // KEY_F10
            { 87, ConsoleKey.F11 },         // KEY_F11
            { 88, ConsoleKey.F12 },         // KEY_F12

            // Нампад
            { 78, ConsoleKey.Add },         // KEY_KPPLUS
            { 74, ConsoleKey.Subtract },    // KEY_KPMINUS
            { 96, ConsoleKey.Enter },       // KEY_KPENTER
            { 82, ConsoleKey.NumPad0 },     // KEY_KP0
            { 79, ConsoleKey.NumPad1 },     // KEY_KP1
            { 80, ConsoleKey.NumPad2 },     // KEY_KP2
            { 81, ConsoleKey.NumPad3 },     // KEY_KP3
            { 75, ConsoleKey.NumPad4 },     // KEY_KP4
            { 76, ConsoleKey.NumPad5 },     // KEY_KP5
            { 77, ConsoleKey.NumPad6 },     // KEY_KP6
            { 71, ConsoleKey.NumPad7 },     // KEY_KP7
            { 72, ConsoleKey.NumPad8 },     // KEY_KP8
            { 73, ConsoleKey.NumPad9 }      // KEY_KP9
        };

        #endregion
}