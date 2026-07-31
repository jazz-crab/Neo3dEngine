namespace Neo3dEngine;

public static class ColorExtensions
{
    private static readonly (ConsoleColor ConsoleColor, byte R, byte G, byte B)[] ConsolePalette = 
    [
        (ConsoleColor.Black,       0,   0,   0),
        (ConsoleColor.DarkBlue,    0,   0, 128),
        (ConsoleColor.DarkGreen,   0, 128,   0),
        (ConsoleColor.DarkCyan,    0, 128, 128),
        (ConsoleColor.DarkRed,    128,   0,   0),
        (ConsoleColor.DarkMagenta,128,   0, 128),
        (ConsoleColor.DarkYellow, 128, 128,   0),
        (ConsoleColor.Gray,       192, 192, 192),
        (ConsoleColor.DarkGray,   128, 128, 128),
        (ConsoleColor.Blue,        0,   0, 255),
        (ConsoleColor.Green,       0, 255,   0),
        (ConsoleColor.Cyan,        0, 255, 255),
        (ConsoleColor.Red,       255,   0,   0),
        (ConsoleColor.Magenta,   255,   0, 255),
        (ConsoleColor.Yellow,    255, 255,   0),
        (ConsoleColor.White,     255, 255, 255)
    ];

    public static ConsoleColor ToConsoleColor(this Color color)
    {
        ConsoleColor closestColor = ConsoleColor.Black;
        int minDistance = int.MaxValue;

        foreach (var p in ConsolePalette)
        {
            int rDiff = color.R - p.R;
            int gDiff = color.G - p.G;
            int bDiff = color.B - p.B;

            int distance = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;

            if (distance < minDistance)
            {
                minDistance = distance;
                closestColor = p.ConsoleColor;

                if (distance == 0) break;
            }
        }

        return closestColor;
    }
}