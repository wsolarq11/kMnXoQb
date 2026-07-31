using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Launchpad;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "WT Launcher";
        ApplySystemBackdrop();
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
}
