using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launchpad.Core.Localization;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Localization;
using Launchpad.UseCases;

namespace Launchpad.ViewModels;

/// <summary>
/// Home screen state: item list, search, selection, theme, language, launch
/// orchestration. Thin shell over UseCases — all decisions live in the testable
/// pure layer. Every visible text is a key lookup into <see cref="LanguageService"/>;
/// a language switch re-raises all property notifications.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ItemUseCase _itemUseCase;
    private readonly LaunchUseCase _launchUseCase;
    private readonly SettingsUseCase _settingsUseCase;
    private readonly IDialogService _dialogs;
    private readonly LanguageService _language;

    private List<LaunchItem> _all = [];

    /// <summary>
    /// Replaced wholesale on refresh (not cleared in place): GridView recycles
    /// containers on Clear/Add and x:Bind OneTime/OneWay leaves stale CheckBox
    /// state on recycled containers, making selections appear to jump around.
    /// </summary>
    private ObservableCollection<LaunchItem> _items = [];

    public ObservableCollection<LaunchItem> Items => _items;

    // Partial properties (C# 13): the source generator emits the setter +
    // change hooks without the AOT-incompatible backing-field pattern
    // (MVVMTK0045).
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AppSettings Settings { get; set; } = new();

    /// <summary>TwoWay-bound to the Confirm toggle; the setter persists through
    /// <see cref="SettingsUseCase"/>. OneWay+Toggled was racy (binding-driven IsOn
    /// changes re-trigger Toggled, flipping state back).</summary>
    [ObservableProperty]
    public partial bool ConfirmEnabled { get; set; }

    public int ItemCount => _all.Count;

    public int SelectedCount => _all.Count(i => i.Selected);

    public string RecentName => Settings.LaunchHistory.FirstOrDefault() ?? "--";

    // --- Localized text (keys resolved through LanguageService) ---
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
    public string LanguageLabel => _language.Label(Settings.Language);

    // Card tooltips: resolved through the root DataContext so DataTemplate
    // bindings can reach them via {Binding DataContext.X, ElementName=Root}.
    public string TooltipEditText => _language[LanguageKey.TooltipEdit];
    public string TooltipDeleteText => _language[LanguageKey.TooltipDelete];
    public string TooltipMoveUpText => _language[LanguageKey.TooltipMoveUp];
    public string TooltipMoveDownText => _language[LanguageKey.TooltipMoveDown];

    /// <summary>True only when the full (unfiltered) list is empty — matches
    /// legacy: the "no items yet" hint never shows for a search with no hits.</summary>
    public bool IsEmpty => _all.Count == 0;

    /// <summary>Search active but nothing matches; the grid shows a "no matches"
    /// hint instead of the misleading empty-state CTA.</summary>
    public bool HasNoMatches => _all.Count > 0 && Items.Count == 0;

    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

    public string Theme => Settings.Theme;

    /// <summary>Glyph shows what the next click does; Auto for the system-following state.</summary>
    public string ThemeGlyph => Settings.Theme switch
    {
        "dark" => LucideGlyph.Sun,
        "light" => LucideGlyph.Moon,
        _ => LucideGlyph.Auto,
    };

    public HomeViewModel(
        ItemUseCase itemUseCase,
        LaunchUseCase launchUseCase,
        SettingsUseCase settingsUseCase,
        IDialogService dialogs,
        LanguageService language)
    {
        _itemUseCase = itemUseCase;
        _launchUseCase = launchUseCase;
        _settingsUseCase = settingsUseCase;
        _dialogs = dialogs;
        _language = language;
        _language.PropertyChanged += OnLanguageChanged;
        Settings = settingsUseCase.Load();
        // The equal-value guard in OnConfirmEnabledChanged keeps this sync
        // side-effect free (no write during construction).
        ConfirmEnabled = Settings.ConfirmEnabled;
        Load();
    }

    partial void OnSearchQueryChanged(string value) => RefreshItems();

    partial void OnSettingsChanged(AppSettings value)
    {
        OnPropertyChanged(nameof(RecentName));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(ThemeGlyph));
        OnPropertyChanged(nameof(LanguageLabel));
        _language.Apply(value.Language);
        // ConfirmEnabled is only ever written through OnConfirmEnabledChanged,
        // which persists into Settings immediately — the two are always in
        // sync, so no mirroring branch is needed here.
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Full refresh: every text property re-evaluates, and the item collection
        // is rebuilt so card bindings (incl. the danger-reason tooltip converter)
        // resolve against the new language.
        RefreshItems();
        OnPropertyChanged(string.Empty);
    }

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    partial void OnConfirmEnabledChanged(bool value)
    {
        if (value == Settings.ConfirmEnabled)
        {
            return;
        }

        Settings = SettingsUseCase.SetConfirmEnabled(Settings, value);
        TrySave();
    }

    private void Load()
    {
        var (result, recoveryNoteKey) = _itemUseCase.LoadItems();
        if (result.IsError)
        {
            StatusText = _language.Format(LanguageKey.StatusConfigError, result.FirstError.Description);
            return;
        }

        _all = result.Value.ToList();
        if (recoveryNoteKey is not null)
        {
            StatusText = _language[recoveryNoteKey.Value];
        }

        RefreshItems();
    }

    private void RefreshItems()
    {
        _items = new ObservableCollection<LaunchItem>(ItemUseCase.Filter(_all, SearchQuery));
        OnPropertyChanged(nameof(Items));

        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoMatches));
    }

    /// <summary>Persist both files; surfaces the first failure in the status bar.</summary>
    private bool TrySave()
    {
        var items = _itemUseCase.SaveItems(_all);
        var settings = _settingsUseCase.Save(Settings);
        if (items.IsError)
        {
            StatusText = _language.Format(LanguageKey.StatusSaveFailed, items.FirstError.Description);
        }
        else if (settings.IsError)
        {
            StatusText = _language.Format(LanguageKey.StatusSaveFailed, settings.FirstError.Description);
        }

        return !items.IsError && !settings.IsError;
    }

    /// <summary>Position of an item reference in the full list (used by the edit flow).</summary>
    public int IndexOf(LaunchItem item) => _all.FindIndex(i => ReferenceEquals(i, item));

    /// <summary>Full unfiltered list, for id-collision checks when building new items.</summary>
    public IReadOnlyList<LaunchItem> AllItems => _all;

    /// <summary>Checkbox click intent: stable id + the target state captured at click time.</summary>
    public sealed record SelectRequest(string Id, bool Target);

    public void ApplyEdit(LaunchItem edited, int? index)
    {
        _all = ItemUseCase.Upsert(_all, edited, index).ToList();
        if (!TrySave())
        {
            return;
        }

        RefreshItems();
        StatusText = _language[index is null ? LanguageKey.StatusAdded : LanguageKey.StatusUpdated];
    }

    /// <summary>Three-state cycle: system (follow OS) → dark → light → system.
    /// "system" maps to ElementTheme.Default, which WinUI resolves to the OS theme.</summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        Settings = Settings.Theme switch
        {
            "system" => SettingsUseCase.SetTheme(Settings, "dark"),
            "dark" => SettingsUseCase.SetTheme(Settings, "light"),
            _ => SettingsUseCase.SetTheme(Settings, "system"),
        };
        TrySave();
    }

    /// <summary>Cycles the language setting: auto → zh-CN → en-US → auto.
    /// "auto" follows the system language; LanguageService re-evaluates and
    /// re-notifies every bound text.</summary>
    [RelayCommand]
    private void ToggleLanguage()
    {
        Settings = SettingsUseCase.SetLanguage(Settings, LanguageService.NextLanguage(Settings.Language));
        TrySave();
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        _all = ItemUseCase.ToggleSelectAll(_all).ToList();
        TrySave();
        RefreshItems();
    }

    /// <summary>
    /// Applies the checkbox state captured at click time. Resolves by id (not
    /// reference): deferred execution may run after a rebuild replaced the item
    /// instance, and target-state semantics make re-invocations idempotent.
    /// </summary>
    [RelayCommand]
    private void SetSelect(SelectRequest request)
    {
        _all = ItemUseCase.SetSelectById(_all, request.Id, request.Target).ToList();
        TrySave();
        RefreshItems();
    }

    [RelayCommand]
    private void MoveUp(LaunchItem item) => MoveBy(item, -1);

    [RelayCommand]
    private void MoveDown(LaunchItem item) => MoveBy(item, 1);

    private void MoveBy(LaunchItem item, int delta)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }

        _all = ItemUseCase.Move(_all, index, delta).ToList();
        TrySave();
        RefreshItems();
    }

    [RelayCommand]
    private void Delete(LaunchItem item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }

        _all = ItemUseCase.Delete(_all, index).ToList();
        TrySave();
        RefreshItems();
        StatusText = _language[LanguageKey.StatusDeleted];
    }

    public bool NeedsConfirm(LaunchItem item) => _launchUseCase.NeedsConfirm(Settings, item);

    public async Task LaunchAsync(LaunchItem item)
    {
        if (NeedsConfirm(item))
        {
            var ok = await _dialogs.ConfirmLaunchAsync(item, dangerReason: null);
            if (!ok)
            {
                return;
            }
        }

        var result = _launchUseCase.TryLaunch(item);
        if (result.IsError)
        {
            StatusText = _language.Format(LanguageKey.StatusLaunchFailed, result.FirstError.Description);
            return;
        }

        Settings = SettingsUseCase.PushHistory(Settings, item.Name);
        TrySave();
        StatusText = _language.Format(LanguageKey.StatusLaunched, item.Name);
    }

    [RelayCommand]
    private void LaunchSelected()
    {
        var selected = _all.Where(i => i.Selected).ToList();
        if (selected.Count == 0)
        {
            StatusText = string.Empty;
            return;
        }

        var confirmItems = _launchUseCase.RequireConfirm(Settings, selected);
        if (confirmItems.Count > 0)
        {
            _ = ConfirmAndLaunchAsync(selected, confirmItems);
            return;
        }

        LaunchSelectedCore(selected);
    }

    private async Task ConfirmAndLaunchAsync(List<LaunchItem> selected, IReadOnlyList<LaunchItem> confirmItems)
    {
        var ok = await _dialogs.ConfirmBatchAsync(confirmItems);
        if (!ok)
        {
            return;
        }

        LaunchSelectedCore(selected);
    }

    private void LaunchSelectedCore(List<LaunchItem> selected)
    {
        var (succeeded, failedIndexes) = _launchUseCase.LaunchMany(selected);
        Settings = SettingsUseCase.PushHistoryMany(Settings, selected, failedIndexes.ToHashSet());
        // Legacy: clear the selection after a batch launch so the same terminals
        // cannot be re-fired by a second click (archive/launchpad-rs batch_launch).
        _all = ItemUseCase.ClearSelection(_all).ToList();
        TrySave();
        RefreshItems();
        StatusText = succeeded == selected.Count
            ? _language.Format(LanguageKey.StatusLaunchedN, succeeded)
            : _language.Format(LanguageKey.StatusLaunchedPartial, succeeded, selected.Count, failedIndexes.Count);
    }
}
