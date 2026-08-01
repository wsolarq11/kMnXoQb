using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Persistent storage for items and settings. Implemented by the app shell.</summary>
public interface IConfigStore
{
    /// <summary>Set when the last ReadItems recovered a corrupt config.json from
    /// the backup; the UI surfaces it in the status bar (null = no recovery).</summary>
    string? LastRecoveryNote { get; }

    IReadOnlyList<LaunchItem> ReadItems();

    AppSettings ReadSettings();

    void WriteItems(IReadOnlyList<LaunchItem> items);

    void WriteSettings(AppSettings settings);
}
