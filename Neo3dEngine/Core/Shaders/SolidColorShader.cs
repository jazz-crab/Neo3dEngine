namespace Neo3dEngine;

public class SolidColorShader(Color color) : IShader
{
    public Color Color { get; set; } = color;
    public Color GetColor(in ShaderContext context) => Color;
}