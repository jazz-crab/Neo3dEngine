namespace Neo3dEngine;
public abstract class Screen
{
    
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    protected Screen(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public abstract void RenderFrame(Scene scene);
    protected abstract Vector2 CalculateUV(int i, int j);
    protected abstract void Present();
    public abstract void DrawText(string text, Vector2Int position, Color color);
}