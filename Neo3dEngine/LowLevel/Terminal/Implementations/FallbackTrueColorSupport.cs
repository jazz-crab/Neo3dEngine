namespace Neo3dEngine.LowLevel;

internal class FallbackTrueColorSupport : ITrueColorSupport
{
    public bool Enable() => false;
}