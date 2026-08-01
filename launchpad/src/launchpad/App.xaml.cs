using Launchpad.Core.Ports;
using Launchpad.Infrastructure;
using Launchpad.Localization;
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
        // The logger itself must never crash the process (disk full, locked
        // temp dir, permissions); logging is best-effort.
        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), "launchpad-crash.log"),
                $"[{DateTime.Now:O}] {e.Exception}\n---\n");
        }
        catch
        {
            // best effort only
        }

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
        services.AddSingleton(sp => LanguageService.FromSettings(sp.GetRequiredService<IConfigStore>().ReadSettings()));
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

        LanguageService.AssignInstance(_services.GetRequiredService<LanguageService>());
        _window = _services.GetRequiredService<MainWindow>();
        var homeView = _services.GetRequiredService<HomeView>();
        _window.Content = homeView;

        // Application.RequestedTheme is immutable after startup; theme lives on the content root.
        // "system" maps to ElementTheme.Default, which follows the OS theme.
        homeView.RequestedTheme = _services.GetRequiredService<HomeViewModel>().Theme switch
        {
            "dark" => ElementTheme.Dark,
            "light" => ElementTheme.Light,
            _ => ElementTheme.Default,
        };

        _window.Activate();

        // DialogService resolves XamlRoot lazily from the host element on each show
        // (XamlRoot is not available right after Activate — it is created during layout).
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        ((DirectoryPickerService)_services.GetRequiredService<IDirectoryPicker>()).Attach(hwnd);
        ((DialogService)_services.GetRequiredService<IDialogService>()).Attach(homeView);
    }
}
