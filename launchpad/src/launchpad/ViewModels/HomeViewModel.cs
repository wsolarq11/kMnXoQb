using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.UseCases;

namespace Launchpad.ViewModels;

/// <summary>
/// Home screen state: item list, search, selection, theme, launch orchestration.
/// All list mutations go through pure <see cref="ItemUseCase"/> functions.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ItemUseCase _itemUseCase;
    private readonly LaunchUseCase _launchUseCase;
    private readonly SettingsUseCase _settingsUseCase;
    private readonly IDialogService _dialogs;

    private List<LaunchItem> _all = [];

    public ObservableCollection<LaunchItem> Items { get; } = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private bool _isDark;

    public int ItemCount => _all.Count;

    public int SelectedCount => _all.Count(i => i.Selected);

    public string RecentName => _settings.LaunchHistory.FirstOrDefault() ?? "--";

    public bool IsEmpty => Items.Count == 0;

    public bool HasStatus => !string.IsNullOrEmpty(_statusText);

    public string ThemeGlyph => _isDark ? LucideGlyph.Sun : LucideGlyph.Moon;

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
        _isDark = _settings.Theme == "dark";
        Load();
    }

    partial void OnSearchQueryChanged(string value) => RefreshItems();

    partial void OnSettingsChanged(AppSettings value) => OnPropertyChanged(nameof(RecentName));

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    partial void OnIsDarkChanged(bool value) => OnPropertyChanged(nameof(ThemeGlyph));

    private void Load()
    {
        _all = _itemUseCase.LoadItems().ToList();
        RefreshItems();
    }

    private void RefreshItems()
    {
        Items.Clear();
        foreach (var item in ItemUseCase.Filter(_all, _searchQuery))
        {
            Items.Add(item);
        }
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void Save()
    {
        _itemUseCase.SaveItems(_all);
        _settingsUseCase.Save(_settings);
    }

    /// <summary>Position of an item reference in the full list (used by the edit flow).</summary>
    public int IndexOf(LaunchItem item) => _all.FindIndex(i => ReferenceEquals(i, item));

    public void ApplyEdit(LaunchItem edited, int? index)
    {
        _all = ItemUseCase.Upsert(_all, edited, index).ToList();
        Save();
        RefreshItems();
        StatusText = index is null ? "Added" : "Updated";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _isDark = !_isDark;
        _settings = SettingsUseCase.SetTheme(_settings, _isDark ? "dark" : "light");
        Save();
    }

    [RelayCommand]
    private void ToggleConfirmEnabled()
    {
        _settings = SettingsUseCase.SetConfirmEnabled(_settings, !_settings.ConfirmEnabled);
        Save();
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        _all = ItemUseCase.ToggleSelectAll(_all).ToList();
        Save();
        RefreshItems();
    }

    [RelayCommand]
    private void ToggleSelect(LaunchItem item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }

        _all = ItemUseCase.ToggleSelect(_all, index).ToList();
        Save();
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
        Save();
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
        Save();
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

        _launchUseCase.Launch(item);
        _settings = SettingsUseCase.PushHistory(_settings, item.Name);
        Save();
        StatusText = $"Launched: {item.Name}";
    }

    [RelayCommand]
    private void LaunchSelected()
    {
        var selected = _all.Where(i => i.Selected).ToList();
        foreach (var item in selected)
        {
            _launchUseCase.Launch(item);
        }

        StatusText = selected.Count > 0 ? $"Launched {selected.Count} items" : string.Empty;
    }
}
