using System;

namespace Neo3dEngine;

internal class UnixTrueColorSupport : ITrueColorSupport
{
    public bool Enable()
    {
        if (Console.IsOutputRedirected) return false;
        
        string colorTerm = Environment.GetEnvironmentVariable("COLORTERM") ?? "";
        if (colorTerm.Equals("truecolor", StringComparison.OrdinalIgnoreCase) ||
            colorTerm.Equals("24bit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        string term = Environment.GetEnvironmentVariable("TERM") ?? "";
        if (term.Contains("direct", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("truecolor", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("24bit", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("kitty", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("alacritty", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        string termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";
        if (termProgram.Equals("iTerm.app", StringComparison.OrdinalIgnoreCase) ||
            termProgram.Equals("Apple_Terminal", StringComparison.OrdinalIgnoreCase) ||
            termProgram.Equals("vscode", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}