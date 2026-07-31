using Launchpad.Core.Models;

namespace Launchpad.Core.Domain;

/// <summary>
/// Pure decision maker: given a launch item and terminal availability, produce the
/// exact argv to spawn. No I/O. Ported 1:1 from the Rust <c>plan_windows</c>, with
/// one fix: directory quotes are escaped in the pwsh/cmd fallback paths.
/// </summary>
public static class LaunchPlanner
{
    public static LaunchPlan PlanWindows(LaunchItem item, bool wtAvailable, bool pwshAvailable)
    {
        var dir = item.Directory;
        var terminal = item.Terminal ?? "pwsh";
        var dangerous = DangerousFlagDetector.IsDangerous(item.Command);

        if (wtAvailable)
        {
            return new LaunchPlan
            {
                Executable = "wt.exe",
                Args = ["new-tab", "-d", dir, terminal, "-NoExit", "-Command", item.Command],
                WorkingDirectory = dir,
                IsDangerous = dangerous,
                TerminalOverride = item.Terminal,
            };
        }

        if (pwshAvailable)
        {
            return new LaunchPlan
            {
                Executable = "pwsh.exe",
                Args = ["-NoExit", "-Command", $"cd '{EscapePwshQuotes(dir)}'; {item.Command}"],
                WorkingDirectory = dir,
                IsDangerous = dangerous,
                TerminalOverride = item.Terminal,
            };
        }

        // cmd.exe does not parse its /k argument with standard argv quoting rules
        // (\" is literal), so a cd-prefixed command breaks whenever the directory
        // contains quotes or spaces (verified by TerminalContractTests). The working
        // directory travels via ProcessStartInfo.WorkingDirectory instead.
        return new LaunchPlan
        {
            Executable = "cmd.exe",
            Args = ["/k", item.Command],
            WorkingDirectory = dir,
            IsDangerous = dangerous,
            TerminalOverride = item.Terminal,
        };
    }

    /// <summary>PowerShell single-quoted strings escape a quote by doubling it.</summary>
    public static string EscapePwshQuotes(string path) => path.Replace("'", "''", StringComparison.Ordinal);
}
