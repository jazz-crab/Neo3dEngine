using System.Runtime.InteropServices;

namespace Neo3dEngine.LowLevel;

internal class WindowsTrueColorSupport : ITrueColorSupport
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public bool Enable()
    {
        if (Console.IsOutputRedirected) return false;

        try
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == IntPtr.Zero || handle == (IntPtr)(-1)) return false;

            if (GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                return SetConsoleMode(handle, mode);
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}