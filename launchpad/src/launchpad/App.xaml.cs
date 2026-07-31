using Launchpad.Core.Ports;
using Launchpad.Infrastructure;
using Launchpad.UseCases;
using Launchpad.ViewModels;
using Launchpad.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Launchpad;

public partial class App : Application
{
    private readonly ServiceProvider _services;
    private Window? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        _services = ConfigureServices();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        File.AppendAllText(
            Path.Combine(Path.GetTempPath(), "launchpad-crash.log"),
            $"[{DateTime.Now:O}] {e.Exception}\n---\n");
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        // D2: 配置目录保留 ../config 相对路径（与 Flutter 版语义一致：从项目根运行）。
        var configDir = Path.GetFullPath(@"..\config");

        services.AddSingleton<IConfigStore>(new ConfigStore(configDir));
        services.AddSingleton<ITerminalDetector, TerminalDetector>();
        services.AddSingleton<IProcessSpawner, ProcessSpawner>();
        services.AddSingleton<IDirectoryChecker, DirectoryChecker>();
        services.AddSingleton<IDirectoryPicker, DirectoryPickerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ItemUseCase>();
        services.AddSingleton<LaunchUseCase>();
        services.AddSingleton<SettingsUseCase>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HomeView>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        var homeView = _services.GetRequiredService<HomeView>();
        _window.Content = homeView;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        ((DirectoryPickerService)_services.GetRequiredService<IDirectoryPicker>()).Attach(hwnd);
        ((DialogService)_services.GetRequiredService<IDialogService>()).Attach(homeView.XamlRoot);

        // Application.RequestedTheme is immutable after startup; theme lives on the content root.
        homeView.RequestedTheme = _services.GetRequiredService<HomeViewModel>().IsDark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        _window.Activate();
    }
}
