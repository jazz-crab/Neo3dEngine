namespace Neo3dEngine;

public interface IShader
{
    Color GetColor(in ShaderContext context);
}