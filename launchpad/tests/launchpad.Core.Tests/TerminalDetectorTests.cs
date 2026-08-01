using Launchpad.Infrastructure;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class TerminalDetectorTests
{
    [Fact]
    public void TerminalAvailable_CachesResultPerName()
    {
        var probeCount = 0;
        bool Probe(string _)
        {
            probeCount++;
            return true;
        }

        var detector = new TerminalDetector(Probe);

        Assert.True(detector.TerminalAvailable("wt.exe"));
        Assert.True(detector.TerminalAvailable("wt.exe"));
        Assert.True(detector.TerminalAvailable("pwsh.exe"));

        Assert.Equal(2, probeCount); // wt cached after first call; pwsh probed once
    }

    [Fact]
    public void TerminalAvailable_CachesNegativeResultsToo()
    {
        var probeCount = 0;
        bool Probe(string _)
        {
            probeCount++;
            return false;
        }

        var detector = new TerminalDetector(Probe);

        Assert.False(detector.TerminalAvailable("wt.exe"));
        Assert.False(detector.TerminalAvailable("wt.exe"));
        Assert.Equal(1, probeCount);
    }
}
