namespace Neo3dEngine;

internal class FallbackTrueColorSupport : ITrueColorSupport
{
    public bool Enable() => false;
}