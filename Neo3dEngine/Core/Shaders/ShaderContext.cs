namespace Neo3dEngine;

public readonly struct ShaderContext(Vector3 worldPosition, 
    Vector3 normal, 
    Vector2 uv, 
    Vector3 rayDirection)
{
    public Vector3 WorldPosition { get; } = worldPosition;
    public Vector3 Normal { get; } = normal;
    public Vector2 UV { get; } = uv;
    public Vector3 RayDirection { get; } = rayDirection;
}