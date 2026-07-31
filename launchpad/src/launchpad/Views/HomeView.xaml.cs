using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;

namespace Launchpad.Views;

public sealed partial class HomeView : Page
{
    private readonly IDirectoryChecker _directoryChecker;
    private readonly IDirectoryPicker _directoryPicker;
    private readonly DispatcherTimer _themeTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private int? _lastAppsUseLightTheme;

    public HomeViewModel ViewModel { get; }

    public HomeView(HomeViewModel viewModel, IDirectoryChecker directoryChecker, IDirectoryPicker directoryPicker)
    {
        ViewModel = viewModel;
        _directoryChecker = directoryChecker;
        _directoryPicker = directoryPicker;
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _lastAppsUseLightTheme = ReadAppsUseLightTheme();
        _themeTimer.Tick += OnThemePollTick;
        _themeTimer.Start();
    }

    /// <summary>
    /// Unpackaged WinUI 3 apps do not react automatically to OS theme switches
    /// while ElementTheme.Default is set, and UISettings events are unreliable
    /// here — so poll the registry (the authoritative source for the Windows
    /// "app mode" setting) and re-apply Default to force a refresh.
    /// </summary>
    private void OnThemePollTick(object? sender, object e)
    {
        var current = ReadAppsUseLightTheme();
        if (current is null || current == _lastAppsUseLightTheme)
        {
            return;
        }

        _lastAppsUseLightTheme = current;
        if (ViewModel.Theme != "system")
        {
            return;
        }

        RequestedTheme = ElementTheme.Light;
        RequestedTheme = ElementTheme.Default;
    }

    private static int? ReadAppsUseLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") as int?;
        }
        catch
        {
            return null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.Theme))
        {
            RequestedTheme = ViewModel.Theme switch
            {
                "dark" => ElementTheme.Dark,
                "light" => ElementTheme.Light,
                _ => ElementTheme.Default, // system：跟随 OS 主题
            };
        }
    }

    private void OnThemeToggle(object sender, RoutedEventArgs e) => ViewModel.ToggleThemeCommand.Execute(null);

    private void OnSelectAll(object sender, RoutedEventArgs e) => ViewModel.ToggleSelectAllCommand.Execute(null);

    private void OnLaunchSelected(object sender, RoutedEventArgs e) => ViewModel.LaunchSelectedCommand.Execute(null);

    private async void OnCardClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LaunchItem item)
        {
            await ViewModel.LaunchAsync(item);
        }
    }

    private void OnNew(object sender, RoutedEventArgs e) => _ = ShowEditAsync(null);

    private void OnEdit(object sender, RoutedEventArgs e) => _ = ShowEditAsync(ItemFrom(sender));

    private void OnDelete(object sender, RoutedEventArgs e) => ViewModel.DeleteCommand.Execute(ItemFrom(sender));

    private void OnMoveUp(object sender, RoutedEventArgs e) => ViewModel.MoveUpCommand.Execute(ItemFrom(sender));

    private void OnMoveDown(object sender, RoutedEventArgs e) => ViewModel.MoveDownCommand.Execute(ItemFrom(sender));

    private void OnSelectToggled(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox)?.DataContext is LaunchItem item)
        {
            ViewModel.ToggleSelectCommand.Execute(item);
        }
    }

    private static LaunchItem ItemFrom(object sender) => (LaunchItem)((FrameworkElement)sender).DataContext;

    private async Task ShowEditAsync(LaunchItem? item)
    {
        var vm = new EditViewModel(_directoryChecker, _directoryPicker, item);
        var dialog = new EditDialog(vm) { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            int? index = item is null ? null : ViewModel.IndexOf(item);
            ViewModel.ApplyEdit(vm.BuildItem(ViewModel.AllItems), index);
        }
        else if (result == ContentDialogResult.Secondary && item is not null)
        {
            ViewModel.DeleteCommand.Execute(item);
        }
    }
}
