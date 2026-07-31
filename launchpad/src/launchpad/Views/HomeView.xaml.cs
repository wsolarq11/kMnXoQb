using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Launchpad.Views;

public sealed partial class HomeView : Page
{
    private readonly IDirectoryChecker _directoryChecker;
    private readonly IDirectoryPicker _directoryPicker;

    public HomeViewModel ViewModel { get; }

    public HomeView(HomeViewModel viewModel, IDirectoryChecker directoryChecker, IDirectoryPicker directoryPicker)
    {
        ViewModel = viewModel;
        _directoryChecker = directoryChecker;
        _directoryPicker = directoryPicker;
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.IsDark))
        {
            RequestedTheme = ViewModel.IsDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void OnConfirmToggled(object sender, RoutedEventArgs e) => ViewModel.ToggleConfirmEnabledCommand.Execute(null);

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
