using ErrorOr;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.UseCases;

/// <summary>Settings orchestration; mutations are pure record updates.
/// Persist failures return structured errors so the UI never silently drops a save.</summary>
public sealed class SettingsUseCase(IConfigStore store)
{
    public AppSettings Load() => store.ReadSettings();

    public ErrorOr<Success> Save(AppSettings settings)
    {
        try
        {
            store.WriteSettings(settings);
            return Result.Success;
        }
        catch (Exception e)
        {
            return StoreErrors.WriteFailed("settings.json", e.Message);
        }
    }

    public static AppSettings SetTheme(AppSettings settings, string theme) => settings with { Theme = theme };

    public static AppSettings SetConfirmEnabled(AppSettings settings, bool enabled) =>
        settings with { ConfirmEnabled = enabled };

    /// <summary>Language setting value: "auto" (follow system), "zh-CN", or "en-US".</summary>
    public static AppSettings SetLanguage(AppSettings settings, string language) =>
        settings with { Language = language };

    public static AppSettings PushHistory(AppSettings settings, string name) =>
        settings with { LaunchHistory = LaunchUseCase.PushHistory(settings.LaunchHistory, name) };

    /// <summary>Push multiple names in order, skipping failed indexes.</summary>
    public static AppSettings PushHistoryMany(
        AppSettings settings,
        IReadOnlyList<LaunchItem> launched,
        IReadOnlySet<int> failedIndexes)
    {
        var current = settings;
        for (var i = 0; i < launched.Count; i++)
        {
            if (!failedIndexes.Contains(i))
            {
                current = PushHistory(current, launched[i].Name);
            }
        }

        return current;
    }
}
