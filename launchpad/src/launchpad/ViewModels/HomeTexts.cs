using System.ComponentModel;
using Launchpad.Core.Localization;
using Launchpad.Localization;

namespace Launchpad.ViewModels;

/// <summary>
/// Static labels of the home screen, resolved through the LanguageService.
/// A language switch re-raises PropertyChanged(null) so every bound text
/// re-evaluates; home screen list/launch state stays in HomeViewModel.
/// </summary>
public sealed class HomeTexts : INotifyPropertyChanged
{
    private readonly LanguageService _language;

    public HomeTexts(LanguageService language)
    {
        _language = language;
        _language.PropertyChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public string ConfirmText => _language[LanguageKey.ToggleConfirm];
    public string NewButtonText => _language[LanguageKey.BtnNew];
    public string SelectAllText => _language[LanguageKey.BtnSelectAll];
    public string LaunchSelectedText => _language[LanguageKey.BtnLaunchSelected];
    public string SearchPlaceholderText => _language[LanguageKey.SearchPlaceholder];
    public string StatItemsText => _language[LanguageKey.StatItems];
    public string StatRecentText => _language[LanguageKey.StatRecent];
    public string EmptyTitleText => _language[LanguageKey.EmptyTitle];
    public string EmptySubtitleText => _language[LanguageKey.EmptySubtitle];
    public string EmptyButtonText => _language[LanguageKey.EmptyButton];
    public string NoMatchTitleText => _language[LanguageKey.NoMatchTitle];
    public string NoMatchSubtitleText => _language[LanguageKey.NoMatchSubtitle];
    public string ThemeTooltip => _language[LanguageKey.TooltipTheme];
    public string LanguageTooltip => _language[LanguageKey.TooltipLanguage];

    // Card tooltips: resolved through the root DataContext so DataTemplate
    // bindings can reach them via {Binding DataContext.Texts.X, ElementName=Root}.
    public string TooltipEditText => _language[LanguageKey.TooltipEdit];
    public string TooltipDeleteText => _language[LanguageKey.TooltipDelete];
    public string TooltipMoveUpText => _language[LanguageKey.TooltipMoveUp];
    public string TooltipMoveDownText => _language[LanguageKey.TooltipMoveDown];

    public event PropertyChangedEventHandler? PropertyChanged;
}
