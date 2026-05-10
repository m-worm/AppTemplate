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
        // Wire exception handlers before anything else so startup crashes are captured
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public override void Initialize()
    {
        LogManager.Setup().LoadConfigurationFromFile("Assets/nlog.config");

        var sc = new ServiceCollection();
        ConfigureServices(sc);
        _services = sc.BuildServiceProvider();
        _logger   = _services.GetRequiredService<ILogger<App>>();

        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
        _logger.LogInformation("Starting | Version={Version} Platform={Platform} OS={OS}",
            version,
            OperatingSystem.IsBrowser() ? "Browser" : "Desktop",
            Environment.OSVersion);

        AvaloniaXamlLoader.Load(this);

        // Run LoadAsync on a thread-pool thread to avoid deadlocking Avalonia's
        // SynchronizationContext when called synchronously during Initialize().
        var settings = _services.GetRequiredService<ISettingsService>();
        Task.Run(() => settings.LoadAsync()).GetAwaiter().GetResult();
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

    private static void ConfigureServices(IServiceCollection services)
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
        services.AddSingleton<IWindowStateService,  WindowStateService>();

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
