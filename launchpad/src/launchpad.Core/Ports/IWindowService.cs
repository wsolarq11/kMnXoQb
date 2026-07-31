using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Window position/size persistence and restore.</summary>
public interface IWindowService
{
    WindowState? Load();

    void Save(WindowState state);
}
