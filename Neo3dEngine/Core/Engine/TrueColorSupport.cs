using System;
using System.Runtime.InteropServices;

namespace Neo3dEngine;

public static class TrueColorSupport
{
    private static readonly ITrueColorSupport _provider;

    static TrueColorSupport()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _provider = new WindowsTrueColorSupport();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _provider = new UnixTrueColorSupport();
        }
        else
        {
            _provider = new FallbackTrueColorSupport();
        }
    }
    
    public static bool Enable()
    {
        try
        {
            return _provider.Enable();
        }
        catch
        {
            return false;
        }
    }
}