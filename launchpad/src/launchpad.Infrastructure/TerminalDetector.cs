using System.Diagnostics;
using Launchpad.Core.Ports;

namespace Launchpad.Infrastructure;

/// <summary>PATH lookup via <c>where</c> (Windows). Equivalent to the legacy Rust which_cmd.</summary>
public sealed class TerminalDetector : ITerminalDetector
{
    public bool TerminalAvailable(string name)
    {
        var psi = new ProcessStartInfo("where", name)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi);
        process!.WaitForExit();
        return process.ExitCode == 0;
    }
}
