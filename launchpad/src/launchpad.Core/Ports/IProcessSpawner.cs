using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Spawns a process from a plan. Zero shell: argv passed verbatim.</summary>
public interface IProcessSpawner
{
    void Launch(LaunchPlan plan);
}
