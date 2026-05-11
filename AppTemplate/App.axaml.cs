using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AppTemplate.Services;
using AppTemplate.ViewModels;
using AppTemplate.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace AppTemplate;

public partial class App : Application
{
    private IServiceProvider? _services;
    private ILogger<App>? _logger;

    public App()
    {
        // AppDomain and TaskScheduler global handlers are not supported in browser WASM
        if (!OperatingSystem.IsBrowser())
        {
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
    }

    public override void Initialize()
    {
        try
        {
            InitializeCore();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[APP INIT FAILED] " + ex.GetType().FullName + ": " + ex.Message);
            if (ex.InnerException != null)
                Console.Error.WriteLine("[INNER] " + ex.InnerException.GetType().FullName + ": " + ex.InnerException.Message);
            throw;
        }
    }

    private void InitializeCore()
    {
        if (OperatingSystem.IsBrowser())
        {
            var config = new NLog.Config.LoggingConfiguration();
            var consoleTarget = new NLog.Targets.ConsoleTarget("console")
            {
                Layout = "${time} [${level:uppercase=true:padding=-5}] ${logger:shortName=true} — ${message} ${exception:format=type,message}"
            };
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, consoleTarget);
            LogManager.Configuration = config;
        }
        else
            LogManager.Setup().LoadConfigurationFromFile("Assets/nlog.config");

        var sc = new ServiceCollection();
        ConfigureServices(sc, OperatingSystem.IsBrowser());
        _services = sc.BuildServiceProvider();
        _logger   = _services.GetRequiredService<ILogger<App>>();

        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
        _logger.LogInformation("Starting | Version={Version} Platform={Platform}",
            version,
            OperatingSystem.IsBrowser() ? "Browser" : "Desktop");

        AvaloniaXamlLoader.Load(this);

        // Load settings - skip sync wait in browser to avoid hangs
        if (!OperatingSystem.IsBrowser())
        {
            var settings = _services.GetRequiredService<ISettingsService>();
            try
            {
                settings.LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load settings during initialization");
            }
        }

        var themeService = _services.GetRequiredService<IThemeService>();
        themeService.ApplyCurrentTheme();
        themeService.ApplyAccentColor();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var mainVm = _services!.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var windowStateService = _services!.GetRequiredService<IWindowStateService>();
            var window = new MainWindow { DataContext = mainVm };

            window.Opened  += async (_, _) => await windowStateService.RestoreAsync(window);
            window.Closing += async (_, _) => await windowStateService.SaveAsync(window);

            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services, bool isBrowser)
    {
        // Logging
        services.AddLogging(b =>
        {
            b.ClearProviders();
            b.AddNLog();
        });

        // Infrastructure services
        services.AddSingleton<IServiceProvider>(sp => sp);
        services.AddSingleton<ISettingsService,    SettingsService>();
        services.AddSingleton<IThemeService,        ThemeService>();
        services.AddSingleton<INavigationService,   NavigationService>();
        if (!isBrowser)
            services.AddSingleton<IWindowStateService, WindowStateService>();

        // ViewModels — singletons so state is preserved across navigation
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HelloWorldViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (_logger != null)
            _logger.LogCritical(ex, "Unhandled domain exception");
        else
            LogManager.GetCurrentClassLogger().Fatal(ex, "Unhandled domain exception (pre-DI)");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
