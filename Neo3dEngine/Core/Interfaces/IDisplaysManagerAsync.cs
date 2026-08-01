namespace Neo3dEngine;
public interface IDisplaysManagerAsync
{
    RenderData FindClosestIntersection(Ray ray, List<IDisplays> displays);
}
