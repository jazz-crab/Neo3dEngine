namespace Neo3dEngine;

internal class LegacyConsolePresenter(int width, int height) : IConsolePresenter
{
    private readonly Dictionary<Color, ConsoleColor> _colorCache = new(64);
    
    private Color _lastColor;
    private ConsoleColor _lastConsoleColor;
    private bool _hasLast;

    public void Present(char[] charBuffer, Color[] colorBuffer)
    {
        int bufferLength = charBuffer.Length;
        
        _colorCache.Clear();
        _hasLast = false;

        int currentIndex = 0;

        while (currentIndex < bufferLength)
        {
            ConsoleColor currentColor = GetConsoleColor(colorBuffer[currentIndex]);
            int runLength = 0;

            while (currentIndex + runLength < bufferLength &&
                   GetConsoleColor(colorBuffer[currentIndex + runLength]) == currentColor)
            {
                runLength++;
            }

            if (Console.ForegroundColor != currentColor)
            {
                Console.ForegroundColor = currentColor;
            }

            Console.Out.Write(charBuffer, currentIndex, runLength);
            currentIndex += runLength;
        }
    }

    private ConsoleColor GetConsoleColor(Color c)
    {
        if (_hasLast && c == _lastColor)
            return _lastConsoleColor;

        if (!_colorCache.TryGetValue(c, out var consoleColor))
        {
            consoleColor = c.ToConsoleColor();
            _colorCache[c] = consoleColor;
        }

        _lastColor = c;
        _lastConsoleColor = consoleColor;
        _hasLast = true;

        return consoleColor;
    }
}