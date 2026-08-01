using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.UseCases;

namespace Launchpad.ViewModels;

/// <summary>
/// Home screen state: item list, search, selection, theme, launch orchestration.
/// Thin shell over UseCases — all decisions live in the testable pure layer.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ItemUseCase _itemUseCase;
    private readonly LaunchUseCase _launchUseCase;
    private readonly SettingsUseCase _settingsUseCase;
    private readonly IDialogService _dialogs;

    private List<LaunchItem> _all = [];

    /// <summary>
    /// Replaced wholesale on refresh (not cleared in place): GridView recycles
    /// containers on Clear/Add and x:Bind OneTime/OneWay leaves stale CheckBox
    /// state on recycled containers, making selections appear to jump around.
    /// </summary>
    private ObservableCollection<LaunchItem> _items = [];

    public ObservableCollection<LaunchItem> Items => _items;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>TwoWay-bound to the Confirm toggle; the setter persists through
    /// <see cref="SettingsUseCase"/>. OneWay+Toggled was racy (binding-driven IsOn
    /// changes re-trigger Toggled, flipping state back).</summary>
    [ObservableProperty]
    private bool _confirmEnabled;

    public int ItemCount => _all.Count;

    public int SelectedCount => _all.Count(i => i.Selected);

    public string RecentName => _settings.LaunchHistory.FirstOrDefault() ?? "--";

    /// <summary>True only when the full (unfiltered) list is empty — matches
    /// legacy: the "no items yet" hint never shows for a search with no hits.</summary>
    public bool IsEmpty => _all.Count == 0;

    /// <summary>Search active but nothing matches; the grid shows a "no matches"
    /// hint instead of the misleading empty-state CTA.</summary>
    public bool HasNoMatches => _all.Count > 0 && Items.Count == 0;

    public bool HasStatus => !string.IsNullOrEmpty(_statusText);

    public string Theme => _settings.Theme;

    /// <summary>Glyph shows what the next click does; Auto for the system-following state.</summary>
    public string ThemeGlyph => _settings.Theme switch
    {
        "dark" => LucideGlyph.Sun,
        "light" => LucideGlyph.Moon,
        _ => LucideGlyph.Auto,
    };

    public HomeViewModel(
        ItemUseCase itemUseCase,
        LaunchUseCase launchUseCase,
        SettingsUseCase settingsUseCase,
        IDialogService dialogs)
    {
        _itemUseCase = itemUseCase;
        _launchUseCase = launchUseCase;
        _settingsUseCase = settingsUseCase;
        _dialogs = dialogs;
        _settings = settingsUseCase.Load();
        _confirmEnabled = _settings.ConfirmEnabled;
        Load();
    }

    partial void OnSearchQueryChanged(string value) => RefreshItems();

    partial void OnSettingsChanged(AppSettings value)
    {
        OnPropertyChanged(nameof(RecentName));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(ThemeGlyph));
        if (_confirmEnabled != value.ConfirmEnabled)
        {
            _confirmEnabled = value.ConfirmEnabled;
        }
    }

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    partial void OnConfirmEnabledChanged(bool value)
    {
        Settings = SettingsUseCase.SetConfirmEnabled(Settings, value);
        TrySave();
    }

    private void Load()
    {
        var (result, recoveryNote) = _itemUseCase.LoadItems();
        if (result.IsError)
        {
            StatusText = $"Config error: {result.FirstError.Description}";
            return;
        }

        _all = result.Value.ToList();
        if (recoveryNote is not null)
        {
            StatusText = recoveryNote;
        }

        RefreshItems();
    }

    private void RefreshItems()
    {
        _items = new ObservableCollection<LaunchItem>(ItemUseCase.Filter(_all, _searchQuery));
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
        var settings = _settingsUseCase.Save(_settings);
        if (items.IsError)
        {
            StatusText = $"Save failed: {items.FirstError.Description}";
        }
        else if (settings.IsError)
        {
            StatusText = $"Save failed: {settings.FirstError.Description}";
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
        StatusText = index is null ? "Added" : "Updated";
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
        StatusText = "Deleted";
    }

    public bool NeedsConfirm(LaunchItem item) => _launchUseCase.NeedsConfirm(_settings, item);

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
            StatusText = $"Launch failed: {result.FirstError.Description}";
            return;
        }

        Settings = SettingsUseCase.PushHistory(Settings, item.Name);
        TrySave();
        StatusText = $"Launched: {item.Name}";
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

        var confirmItems = _launchUseCase.RequireConfirm(_settings, selected);
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
            ? $"Launched {succeeded} items"
            : $"Launched {succeeded} of {selected.Count} items ({failedIndexes.Count} failed)";
    }
}
