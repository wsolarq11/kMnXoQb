using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.Infrastructure;

/// <summary>
/// Persists window bounds into settings.json window_state; the WinUI host
/// applies it on launch and captures it on close.
/// </summary>
public sealed class WindowStateService(IConfigStore store) : IWindowService
{
    public WindowState? Load() => store.ReadSettings().WindowState;

    public void Save(WindowState state)
    {
        var settings = store.ReadSettings();
        store.WriteSettings(settings with { WindowState = state });
    }
}
