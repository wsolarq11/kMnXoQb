using System.ComponentModel;
using Launchpad.Core.Localization;
using Launchpad.Core.Models;

namespace Launchpad.Localization;

/// <summary>
/// Imperative-shell language state: combines the settings value ("auto" /
/// "zh-CN" / "en-US") with the system language. Auto follows the system;
/// an explicit setting wins. Changing the language raises a full
/// PropertyChanged(null) so every bound text re-evaluates.
/// </summary>
public sealed class LanguageService : INotifyPropertyChanged
{
    private static LanguageService? _instance;

    /// <summary>Process-wide instance for code paths outside DI (converters).
    /// Assigned once by App.OnLaunched.</summary>
    public static LanguageService Instance => _instance
        ?? throw new InvalidOperationException("LanguageService.Instance is not assigned yet (App.OnLaunched).");

    public static void AssignInstance(LanguageService service) => _instance = service;

    private AppLanguage _effective;

    public LanguageService(string? languageSetting)
    {
        _effective = Translations.Effective(Translations.Resolve(languageSetting), DetectSystem());
    }

    public static LanguageService FromSettings(AppSettings settings) => new(settings.Language);

    public AppLanguage Current => _effective;

    public string this[LanguageKey key] => Translations.T(key, _effective);

    public string Format(LanguageKey key, params object[] args) =>
        Translations.Format(key, _effective, args);

    /// <summary>Cycles auto → zh-CN → en-US → auto; returns the next settings value.</summary>
    public static string NextLanguage(string? current) => Translations.Resolve(current) switch
    {
        AppLanguage.ZhCn => "en-US",
        AppLanguage.EnUs => "auto",
        _ => "zh-CN",
    };

    /// <summary>Label of the current setting value, for the language toggle button.</summary>
    public string Label(string? setting) => Translations.Resolve(setting) switch
    {
        AppLanguage.ZhCn => this[LanguageKey.LanguageZh],
        AppLanguage.EnUs => this[LanguageKey.LanguageEn],
        _ => this[LanguageKey.LanguageAuto],
    };

    public void Apply(string? languageSetting)
    {
        var next = Translations.Effective(Translations.Resolve(languageSetting), DetectSystem());
        if (next == _effective)
        {
            return;
        }

        _effective = next;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    /// <summary>First entry of the Windows preferred-language list; zh* selects
    /// Chinese, anything else falls back to English. Best effort: any failure
    /// (e.g. restricted environment) also falls back to English.</summary>
    private static AppLanguage DetectSystem()
    {
        try
        {
            return Translations.FromSystemLanguage(
                Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault());
        }
        catch
        {
            return AppLanguage.EnUs;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
