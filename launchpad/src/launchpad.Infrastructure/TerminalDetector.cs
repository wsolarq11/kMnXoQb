using System.Diagnostics;
using Launchpad.Core.Ports;

namespace Launchpad.Infrastructure;

/// <summary>
/// PATH lookup via <c>where</c> (Windows). Equivalent to the legacy Rust which_cmd.
/// Results are cached for the process lifetime: the probe runs once per terminal
/// name (a synchronous child process), so repeated Plan() calls in a session do
/// not spawn where.exe every launch. A PATH change requires an app restart,
/// which is acceptable for a launcher tool.
/// </summary>
public sealed class TerminalDetector : ITerminalDetector
{
    private readonly Func<string, bool> _probe;
    private readonly Dictionary<string, bool> _cache = [];

    /// <param name="probe">Override for tests; defaults to running <c>where</c>.</param>
    public TerminalDetector(Func<string, bool>? probe = null)
    {
        _probe = probe ?? ProbeWhere;
    }

    public bool TerminalAvailable(string name)
    {
        if (!_cache.TryGetValue(name, out var available))
        {
            available = _probe(name);
            _cache[name] = available;
        }

        return available;
    }

    private static bool ProbeWhere(string name)
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
