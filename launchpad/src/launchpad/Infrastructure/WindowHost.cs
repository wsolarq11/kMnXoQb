using Microsoft.UI.Xaml;

namespace Launchpad.Infrastructure;

/// <summary>Owner window handle for unpackaged WinUI interop (folder picker).</summary>
public interface IWindowHandleProvider
{
    IntPtr WindowHandle { get; }
}

/// <summary>Lazily resolved XamlRoot for ContentDialogs; XamlRoot is created
/// during layout, after Window.Activate, so it is read on demand.</summary>
public interface IXamlRootProvider
{
    XamlRoot? CurrentXamlRoot { get; }
}

/// <summary>
/// Window host state filled by the composition root after Activate: the owner
/// HWND and the content element whose XamlRoot backs dialogs. UI services take
/// the narrow interfaces by constructor injection, so no service needs to be
/// attached post-registration and the composition root never casts.
/// </summary>
public sealed class WindowHost : IWindowHandleProvider, IXamlRootProvider
{
    public IntPtr WindowHandle { get; set; }

    public Func<XamlRoot?>? XamlRootSource { get; set; }

    public XamlRoot? CurrentXamlRoot => XamlRootSource?.Invoke();
}
