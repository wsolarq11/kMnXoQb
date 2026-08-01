using Launchpad.Core.Localization;
using Launchpad.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Launchpad.Views;

/// <summary>
/// LanguageKey -> current-language text. Translates through the process-wide
/// LanguageService; item rebuilds on language switch re-evaluate bindings,
/// so the card tooltips pick up the new language on the next refresh.
/// </summary>
public sealed class LanguageKeyTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is LanguageKey key ? LanguageService.Instance[key] : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Selected -> card border thickness (2px when selected, else 1px).</summary>
public sealed class SelectedBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? new Thickness(2) : new Thickness(1);

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Selected -> accent border brush, else the theme border brush.</summary>
public sealed class SelectedBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true
            ? Application.Current.Resources["AccentBrush"]
            : Application.Current.Resources["BorderBrush"];

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Tag -> "#tag" (empty string when null).</summary>
public sealed class TagPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string tag ? $"#{tag}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Group -> "@group" (empty string when null).</summary>
public sealed class GroupPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string group ? $"@{group}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
