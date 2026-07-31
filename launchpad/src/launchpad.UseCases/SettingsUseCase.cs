using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.UseCases;

/// <summary>Settings orchestration; mutations are pure record updates.</summary>
public sealed class SettingsUseCase(IConfigStore store)
{
    public AppSettings Load() => store.ReadSettings();

    public void Save(AppSettings settings) => store.WriteSettings(settings);

    public static AppSettings SetTheme(AppSettings settings, string theme) => settings with { Theme = theme };

    public static AppSettings SetConfirmEnabled(AppSettings settings, bool enabled) =>
        settings with { ConfirmEnabled = enabled };

    public static AppSettings PushHistory(AppSettings settings, string name) =>
        settings with { LaunchHistory = LaunchUseCase.PushHistory(settings.LaunchHistory, name) };
}
