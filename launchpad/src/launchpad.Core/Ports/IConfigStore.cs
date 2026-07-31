using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Persistent storage for items and settings. Implemented by the app shell.</summary>
public interface IConfigStore
{
    IReadOnlyList<LaunchItem> ReadItems();

    AppSettings ReadSettings();

    void WriteItems(IReadOnlyList<LaunchItem> items);

    void WriteSettings(AppSettings settings);
}
