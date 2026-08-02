namespace Neo3dEngine;

public class MyGradientShader : IShader
{
    public Color GetColor(in ShaderContext context)
    {
        byte r = (byte)Math.Clamp(context.UV.X * 255, 0, 255);
        byte g = (byte)Math.Clamp(context.UV.Y * 255, 0, 255);
        return new Color(r, g, 0);
    }
}