using System.Text;

namespace Neo3dEngine;
public class CPUConsoleScreen : Screen
{
    private const string Gradient = " .:!/r(l1Z4H9W8$@";
    
    private readonly int[] _brightnessBuffer;
    private readonly Color[] _colorBuffer;
    private readonly char[] _charBuffer;
    
    private readonly StringBuilder _ansiBuffer;

    private readonly float _aspectRatio;

    public bool IsTrueColorActive { get; private set; }
    
    public CPUConsoleScreen() : base(Console.WindowWidth, Console.WindowHeight)
    {
        IsTrueColorActive = TrueColorSupport.Enable(); 
        
        _brightnessBuffer = new int[Width * Height];
        _colorBuffer = new Color[Width * Height];
        _charBuffer = new char[Width * Height];
        _ansiBuffer = new StringBuilder(Width * Height * 20);
        Console.CursorVisible = false;

        float windowAspect = (float)Width / Height;
        float pixelAspect = 11.0f / 24.0f;
        _aspectRatio = windowAspect * pixelAspect;
    }

    protected override Vector2 CalculateUV(int i, int j)
    {
        Vector2 uv = new Vector2((float)i / (Width - 1), (float)j / (Height - 1)) * 2 - 1;
        uv.X *= _aspectRatio;
        uv.Y = -uv.Y;
        return uv;
    }

    public override void DrawText(string text, Vector2Int position, Color color)
    {
        try
        {
            Console.SetCursorPosition(position.X, position.Y);

            if (IsTrueColorActive)
            {
                Console.Write($"\x1b[38;2;{color.R};{color.G};{color.B}m{text}\x1b[0m");
            }
            else
            {
                Console.ForegroundColor = color.ToConsoleColor();
                Console.Write(text);
            }
        }
        catch { }
    }
    
    public override void RenderFrame(Scene scene)
    {
        Parallel.For(0, Height, j =>
        {
            for (int i = 0; i < Width; i++)
            {
                Vector2 uv = CalculateUV(i, j);
                var pixelData = scene.GetPixelData(uv);

                int index = j * Width + i;
                _brightnessBuffer[index] = pixelData.Brightness;
                _colorBuffer[index] = pixelData.Color;
            }
        });

        Parallel.For(0, _brightnessBuffer.Length, i =>
        {
            int brightness = _brightnessBuffer[i];
            brightness = int.Clamp(brightness, 0, Gradient.Length - 1);
            _charBuffer[i] = Gradient[brightness];
        });

        var uiElements = scene.UI.GetElements();
        foreach (var element in uiElements)
        {
            DrawTextToBuffer(element.Text, element.Position, element.Color);
        }

        Present();
    }
    
    private void DrawTextToBuffer(string text, Vector2Int pos, Color color)
    {
        for (int i = 0; i < text.Length; i++)
        {
            int x = pos.X + i;
            int y = pos.Y;

            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int index = y * Width + x;
                _charBuffer[index] = text[i];
                _colorBuffer[index] = color;
            }
        }
    }
    
    protected override void Present()
    {
        Console.SetCursorPosition(0, 0);

        if (IsTrueColorActive)
        {
            PresentTrueColor();
        }
        else
        {
            PresentLegacyConsoleColor();
        }
    }
    
    private void PresentTrueColor()
    {
        _ansiBuffer.Clear();

        int bufferLength = _charBuffer.Length;
        int currentIndex = 0;

        Color lastColor = default;
        bool isFirst = true;

        while (currentIndex < bufferLength)
        {
            Color currentColor = _colorBuffer[currentIndex];
            int runLength = 0;

            while (currentIndex + runLength < bufferLength &&
                   _colorBuffer[currentIndex + runLength] == currentColor)
            {
                runLength++;
            }

            if (isFirst || currentColor != lastColor)
            {
                _ansiBuffer.Append("\x1b[38;2;")
                    .Append(currentColor.R).Append(';')
                    .Append(currentColor.G).Append(';')
                    .Append(currentColor.B).Append('m');
                
                lastColor = currentColor;
                isFirst = false;
            }

            _ansiBuffer.Append(_charBuffer, currentIndex, runLength);
            currentIndex += runLength;
        }

        _ansiBuffer.Append("\x1b[0m");

        Console.Out.Write(_ansiBuffer.ToString());
    }
    
    private void PresentLegacyConsoleColor()
    {
        Console.SetCursorPosition(0, 0);

        int bufferLength = _charBuffer.Length;
        int currentIndex = 0;

        while (currentIndex < bufferLength)
        {
            ConsoleColor currentColor = _colorBuffer[currentIndex].ToConsoleColor();
            int runLength = 0;

            while (currentIndex + runLength < bufferLength &&
                   _colorBuffer[currentIndex + runLength].ToConsoleColor() == currentColor)
            {
                runLength++;
            }

            if (Console.ForegroundColor != currentColor)
            {
                Console.ForegroundColor = currentColor;
            }

            Console.Out.Write(_charBuffer, currentIndex, runLength);
            currentIndex += runLength;
        }
    }
}
