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
        // Keep the instance alive; unhandled exceptions are logged, not fatal.
        e.Handled = true;
    }

    private static string ResolveConfigDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "config")))
            {
                return Path.Combine(dir.FullName, "config");
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "config");
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        // D2: 配置目录沿用项目根的 config/（与 Flutter 版一致）。
        // 不依赖进程工作目录：从 exe 位置向上搜索含 config/ 的祖先目录。
        var configDir = ResolveConfigDir();

        services.AddSingleton<IConfigStore>(new ConfigStore(configDir));
        services.AddSingleton<ITerminalDetector, TerminalDetector>();
        services.AddSingleton<IProcessSpawner, ProcessSpawner>();
        services.AddSingleton<IDirectoryChecker, DirectoryChecker>();
        services.AddSingleton<IDirectoryPicker, DirectoryPickerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowStateService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ItemUseCase>();
        services.AddSingleton<LaunchUseCase>();
        services.AddSingleton<SettingsUseCase>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HomeView>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var singleInstance = new SingleInstance();
        if (!singleInstance.IsPrimary)
        {
            Exit();
            return;
        }

        _window = _services.GetRequiredService<MainWindow>();
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
