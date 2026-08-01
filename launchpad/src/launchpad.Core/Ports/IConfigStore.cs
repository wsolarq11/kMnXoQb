using Launchpad.Core.Localization;
using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Persistent storage for items and settings. Implemented by the app shell.</summary>
public interface IConfigStore
{
    /// <summary>Language-independent key set when the last ReadItems recovered a
    /// corrupt config.json from the backup; the UI translates and surfaces it in
    /// the status bar (null = no recovery).</summary>
    LanguageKey? LastRecoveryNoteKey { get; }

    IReadOnlyList<LaunchItem> ReadItems();

    AppSettings ReadSettings();

    void WriteItems(IReadOnlyList<LaunchItem> items);

    void WriteSettings(AppSettings settings);
}
