using System.Text;

namespace Neo3dEngine.LowLevel;

internal class TrueColorConsolePresenter(int width, int height) : IConsolePresenter
{
    private readonly StringBuilder _ansiBuffer = new StringBuilder(width * height * 20);
    
    public void Present(char[] charBuffer, Color[] colorBuffer)
    {
        _ansiBuffer.Clear();

        int bufferLength = charBuffer.Length;
        int currentIndex = 0;
        Color lastColor = default;
        bool isFirst = true;

        while (currentIndex < bufferLength)
        {
            Color currentColor = colorBuffer[currentIndex];
            int runLength = 0;

            while (currentIndex + runLength < bufferLength &&
                   colorBuffer[currentIndex + runLength] == currentColor)
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

            _ansiBuffer.Append(charBuffer, currentIndex, runLength);
            currentIndex += runLength;
        }
        _ansiBuffer.Append("\x1b[0m");
        Console.Out.Write(_ansiBuffer.ToString());
    }
}