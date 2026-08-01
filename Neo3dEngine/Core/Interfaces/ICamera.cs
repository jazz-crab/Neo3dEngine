namespace Neo3dEngine;

internal interface ICamera
{
    Ray GetRayForUv(Vector2 uv);
}