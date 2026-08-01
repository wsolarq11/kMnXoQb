using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Launchpad.Infrastructure;

/// <summary>
/// ContentDialog-backed confirmation. Requires an XamlRoot; the host element is
/// captured at attach time and the XamlRoot is resolved lazily on each show —
/// XamlRoot is not available right after Window.Activate (it is created during
/// layout), so reading it at attach time yields null.
/// </summary>
public sealed class DialogService : IDialogService
{
    private FrameworkElement? _host;

    public void Attach(FrameworkElement host) => _host = host;

    public async Task<bool> ConfirmLaunchAsync(LaunchItem item, string? dangerReason)
    {
        var xamlRoot = GuardXamlRoot();
        var dangerText = dangerReason ?? DangerousFlagDetector.DangerousReason(item.Command);
        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = $"Name: {item.Name}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                new TextBlock { Text = $"Command: {item.Command}", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") },
                new TextBlock { Text = $"Directory: {item.Directory}" },
            },
        };
        if (dangerText is not null)
        {
            content.Children.Add(new TextBlock
            {
                Text = dangerText,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerBrush"],
                FontSize = 12,
            });
        }

        var dialog = new ContentDialog
        {
            Title = "Confirm Launch",
            Content = content,
            PrimaryButtonText = "Launch",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Resolves the XamlRoot lazily: by the time a confirmation is shown the host
    /// element is loaded into the visual tree, so XamlRoot is guaranteed non-null.
    /// </summary>
    private XamlRoot GuardXamlRoot()
    {
        var root = _host?.XamlRoot
            ?? throw new InvalidOperationException(
                "DialogService.Attach was not called, or the host element is not loaded (must Attach the window content).");
        return root;
    }

    public async Task<bool> ConfirmDeleteAsync(LaunchItem item)
    {
        var xamlRoot = GuardXamlRoot();
        var dialog = new ContentDialog
        {
            Title = "Delete Item",
            Content = new TextBlock
            {
                Text = $"Delete '{item.Name}'?\nThis cannot be undone.",
                TextWrapping = TextWrapping.Wrap,
            },
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>Batch confirmation listing every item that needs confirmation.</summary>
    public async Task<bool> ConfirmBatchAsync(IReadOnlyList<LaunchItem> items)
    {
        var xamlRoot = GuardXamlRoot();
        var panel = new StackPanel { Spacing = 6 };
        foreach (var item in items)
        {
            var dangerText = DangerousFlagDetector.DangerousReason(item.Command);
            var text = new TextBlock
            {
                Text = $"{item.Name}: {item.Command}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            };
            if (dangerText is not null)
            {
                text.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerBrush"];
            }

            panel.Children.Add(text);
        }

        var dialog = new ContentDialog
        {
            Title = $"Confirm {items.Count} launches",
            Content = panel,
            PrimaryButtonText = "Launch All",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
