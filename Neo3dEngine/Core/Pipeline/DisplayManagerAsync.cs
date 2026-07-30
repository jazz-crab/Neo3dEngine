namespace Neo3dEngine;
public class DisplayManagerAsync : IDisplaysManagerAsync
{
    public RenderData FindClosestIntersection(Ray ray, List<IDisplays> displays)
    {
        RenderData closestData = RenderData.NoRender;

        foreach (var display in displays)
        {
            var currentData = display.GetRenderData(ray);
            if (currentData.Intersection > -1)
            {
                if (closestData.Intersection == -1 || currentData.Intersection < closestData.Intersection)
                {
                    closestData = currentData;
                }
            }
        }
        return closestData;
    }
}
