namespace Neo3dEngine;

public readonly struct Color(byte r, byte g, byte b, byte a = 255)
{
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;
    public byte A { get; } = a;
    
    public static readonly Color Black       = new(0, 0, 0);
    public static readonly Color DarkBlue    = new(0, 0, 128);
    public static readonly Color DarkGreen   = new(0, 128, 0);
    public static readonly Color DarkCyan    = new(0, 128, 128);
    public static readonly Color DarkRed     = new(128, 0, 0);
    public static readonly Color DarkMagenta = new(128, 0, 128);
    public static readonly Color DarkYellow  = new(128, 128, 0);
    public static readonly Color Gray        = new(192, 192, 192);
    public static readonly Color DarkGray    = new(128, 128, 128);
    public static readonly Color Blue        = new(0, 0, 255);
    public static readonly Color Green       = new(0, 255, 0);
    public static readonly Color Cyan        = new(0, 255, 255);
    public static readonly Color Red         = new(255, 0, 0);
    public static readonly Color Magenta     = new(255, 0, 255);
    public static readonly Color Yellow      = new(255, 255, 0);
    public static readonly Color White       = new(255, 255, 255);
    
    public static bool operator ==(Color a, Color b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B;
    }
    
    public static bool operator !=(Color a, Color b)
    {
        return !(a == b);
    }
}