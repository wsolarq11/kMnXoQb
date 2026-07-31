using Launchpad.Infrastructure;
using Xunit;

namespace Launchpad.IntegrationTests;

/// <summary>
/// xUnit v2 has no dynamic skip; this attribute computes Skip at reflection time,
/// so the wt.exe contract test runs locally but is skipped on CI runners (which
/// do not ship Windows Terminal).
/// </summary>
public sealed class WtFactAttribute : FactAttribute
{
    public WtFactAttribute()
    {
        if (!TerminalDetectorAvailable())
        {
            Skip = "wt.exe not available on this machine; run locally for the wt contract check";
        }
    }

    private static bool TerminalDetectorAvailable()
    {
        try
        {
            return new TerminalDetector().TerminalAvailable("wt.exe");
        }
        catch
        {
            return false;
        }
    }
}
