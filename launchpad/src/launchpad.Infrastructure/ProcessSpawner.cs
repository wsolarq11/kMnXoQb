using System.Diagnostics;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.Infrastructure;

/// <summary>
/// Zero-shell spawn: every argv element goes through ArgumentList, never a shell
/// string. The legacy Rust version passed CREATE_NEW_CONSOLE for pwsh/cmd; that flag
/// is unnecessary here because a GUI host has no console to inherit, so Windows
/// allocates a fresh console window for console children automatically.
/// </summary>
public sealed class ProcessSpawner : IProcessSpawner
{
    public void Launch(LaunchPlan plan)
    {
        var psi = new ProcessStartInfo
        {
            FileName = plan.Executable,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
        };
        foreach (var arg in plan.Args)
        {
            psi.ArgumentList.Add(arg);
        }

        Process.Start(psi);
    }
}
