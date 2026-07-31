using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Launchpad;

public sealed partial class MainWindow : Window
{
    private readonly IWindowService _windowState;
    private AppWindow? _appWindow;

    public MainWindow(IWindowService windowState)
    {
        _windowState = windowState;
        InitializeComponent();
        Title = "WT Launcher";
        ApplySystemBackdrop();
        RestoreWindowState();
        Closed += OnClosed;
    }

    /// <summary>
    /// Mica is Windows 11 (22000+) only; fall back to Acrylic on Windows 10,
    /// which the Win10 LTSC target (19044) can render natively.
    /// </summary>
    private void ApplySystemBackdrop()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
    }

    private void RestoreWindowState()
    {
        var state = _windowState.Load();
        if (state is null)
        {
            return;
        }

        _appWindow = AppWindow;
        _appWindow.MoveAndResize(new RectInt32(state.X, state.Y, (int)state.Width, (int)state.Height));
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _appWindow ??= AppWindow;
        _windowState.Save(new WindowState
        {
            X = _appWindow.Position.X,
            Y = _appWindow.Position.Y,
            Width = (uint)_appWindow.Size.Width,
            Height = (uint)_appWindow.Size.Height,
        });
    }

}
