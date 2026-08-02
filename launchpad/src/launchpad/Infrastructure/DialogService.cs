using Launchpad.Core.Domain;
using Launchpad.Core.Localization;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Launchpad.Infrastructure;

/// <summary>
/// ContentDialog-backed confirmation. Requires an XamlRoot; the host element's
/// XamlRoot is resolved lazily on each show via <see cref="IXamlRootProvider"/> —
/// XamlRoot is not available right after Window.Activate (it is created during
/// layout), so the provider must be read at show time, not at construction.
/// Dialog text is translated at show time from the current language; a dialog
/// stays in the language it was opened with (modal, no switching mid-show).
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly LanguageService _language;
    private readonly IXamlRootProvider _xamlRootProvider;

    public DialogService(LanguageService language, IXamlRootProvider xamlRootProvider)
    {
        _language = language;
        _xamlRootProvider = xamlRootProvider;
    }

    public async Task<bool> ConfirmLaunchAsync(LaunchItem item, string? dangerReason)
    {
        var xamlRoot = GuardXamlRoot();
        var dangerText = dangerReason ?? (item.DangerReason is { } key ? _language[key] : null);
        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = _language.Format(LanguageKey.DialogLabelName, item.Name), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                new TextBlock { Text = _language.Format(LanguageKey.DialogLabelCommand, item.Command), FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") },
                new TextBlock { Text = _language.Format(LanguageKey.DialogLabelDirectory, item.Directory) },
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
            Title = _language[LanguageKey.DialogConfirmLaunchTitle],
            Content = content,
            PrimaryButtonText = _language[LanguageKey.BtnLaunch],
            CloseButtonText = _language[LanguageKey.BtnCancel],
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
        var root = _xamlRootProvider.CurrentXamlRoot
            ?? throw new InvalidOperationException(
                "WindowHost.XamlRootSource is not set, or the host element is not loaded (composition root must assign it).");
        return root;
    }

    public async Task<bool> ConfirmDeleteAsync(LaunchItem item)
    {
        var xamlRoot = GuardXamlRoot();
        var dialog = new ContentDialog
        {
            Title = _language[LanguageKey.DialogDeleteItemTitle],
            Content = new TextBlock
            {
                Text = _language.Format(LanguageKey.DialogDeleteItemMessage, item.Name),
                TextWrapping = TextWrapping.Wrap,
            },
            PrimaryButtonText = _language[LanguageKey.BtnDelete],
            CloseButtonText = _language[LanguageKey.BtnCancel],
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
            var dangerKey = DangerousFlagDetector.DangerousReason(item.Command);
            var text = new TextBlock
            {
                Text = $"{item.Name}: {item.Command}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            };
            if (dangerKey is not null)
            {
                text.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerBrush"];
            }

            panel.Children.Add(text);
        }

        var dialog = new ContentDialog
        {
            Title = _language.Format(LanguageKey.DialogBatchTitle, items.Count),
            Content = panel,
            PrimaryButtonText = _language[LanguageKey.BtnLaunchAll],
            CloseButtonText = _language[LanguageKey.BtnCancel],
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
