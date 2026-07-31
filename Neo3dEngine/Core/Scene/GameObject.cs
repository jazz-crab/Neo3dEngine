namespace Neo3dEngine;

public abstract class GameObject(Vector3 position, Vector3 localRotate)
{
    public Vector3 Position = position;
    public Vector3 GlobalRotate = Vector3.Zero;
    public Vector3 LocalRotate = localRotate;
    public Color Color { get; set; } = Color.White;
}