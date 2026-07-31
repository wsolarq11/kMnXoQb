using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Launchpad.Infrastructure;

/// <summary>ContentDialog-backed confirmation. Requires an XamlRoot, supplied once by the host window.</summary>
public sealed class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;

    public void Attach(XamlRoot root) => _xamlRoot = root;

    public async Task<bool> ConfirmLaunchAsync(LaunchItem item, string? dangerReason)
    {
        GuardXamlRoot();
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
            XamlRoot = _xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Guards against a null XamlRoot: Attach happens after Activate in OnLaunched,
    /// but a misordered host would otherwise surface a cryptic ArgumentException
    /// from ContentDialog.ShowAsync.
    /// </summary>
    private void GuardXamlRoot()
    {
        if (_xamlRoot is null)
        {
            throw new InvalidOperationException(
                "DialogService.Attach was not called with a valid XamlRoot (host must Attach after window Activate).");
        }
    }

    /// <summary>Batch confirmation listing every item that needs confirmation.</summary>
    public async Task<bool> ConfirmBatchAsync(IReadOnlyList<LaunchItem> items)
    {
        GuardXamlRoot();
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
            XamlRoot = _xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
