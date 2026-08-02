namespace Neo3dEngine;

public class ChessShader : IShader
{
    public Color GetColor(in ShaderContext context)
    {
        float x = Math.Abs(context.WorldPosition.X) * 2;
        float z = Math.Abs(context.WorldPosition.Z) * 2;
        if ((Math.Abs(x % 2 - 1) < 0.5 && Math.Abs(z % 2 - 1) < 0.5) || (Math.Abs(x % 2) < 0.5 && Math.Abs(z % 2) < 0.5))
        {
            return Color.White;
        }
        return Color.Black;
    }
}