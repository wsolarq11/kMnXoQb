using System.Runtime.InteropServices;
using Launchpad.Core.Domain;
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
    private AppWindow _appWindow = null!;
    private RectInt32? _lastNormalRect;

    public MainWindow(IWindowService windowState)
    {
        _windowState = windowState;
        InitializeComponent();
        Title = "WT Launcher";
        ApplySystemBackdrop();
        _appWindow = AppWindow;
        _appWindow.Changed += OnAppWindowChanged;
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

    /// <summary>Track the last normal (non-minimized) bounds; OnClosed uses this so a
    /// minimized close never persists the offscreen -32000 coordinates.</summary>
    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
        {
            _lastNormalRect = new RectInt32(sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height);
        }
    }

    private void RestoreWindowState()
    {
        var state = _windowState.Load();
        if (state is null)
        {
            return;
        }

        var (left, top, width, height) = GetVirtualScreen();
        var clamped = WindowPosition.ClampToVisible(state, left, top, width, height);
        _appWindow.MoveAndResize(new RectInt32(clamped.X, clamped.Y, (int)clamped.Width, (int)clamped.Height));
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_lastNormalRect is null)
        {
            return;
        }

        _windowState.Save(new WindowState
        {
            X = _lastNormalRect.Value.X,
            Y = _lastNormalRect.Value.Y,
            Width = (uint)_lastNormalRect.Value.Width,
            Height = (uint)_lastNormalRect.Value.Height,
        });
    }

    private static (int Left, int Top, int Width, int Height) GetVirtualScreen() => (
        GetSystemMetrics(SM_XVIRTUALSCREEN),
        GetSystemMetrics(SM_YVIRTUALSCREEN),
        GetSystemMetrics(SM_CXVIRTUALSCREEN),
        GetSystemMetrics(SM_CYVIRTUALSCREEN));

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
