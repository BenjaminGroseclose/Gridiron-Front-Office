using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Desktop.Diagnostics;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;
using Serilog.Extensions.Logging;

namespace GridironFrontOffice.Desktop;

/// <summary>
/// The composition root (ARCHITECTURE.md §6.1, LOGGING.md §4): paths → settings (before DI, so the log level can come
/// from them) → logging → startup banner → failure hooks → services → window. The single-instance guard and the GFO
/// application services arrive in M0 WP9.
/// </summary>
internal sealed partial class App : System.Windows.Application
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    private ServiceProvider? _services;
    private SettingsStore? _settings;
    private Microsoft.Extensions.Logging.ILogger _log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    private CrashHandler? _crash;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = AppPaths.ForCurrentUser().EnsureCreated();
        _settings = SettingsStore.Load(paths);
        var app = AppIdentity.From(typeof(App).Assembly, Configuration);

        Log.Logger = GfoLogging.CreateLogger(paths, _settings.Current, app, debugOutput: Debugger.IsAttached);
        var loggers = new SerilogLoggerFactory(Log.Logger);
        _log = loggers.CreateLogger("GridironFrontOffice.Desktop.App");
        var banner = StartupBanner.Build(app, HostEnvironment.Describe(), paths, _settings.Current);
        HostLog.Banner(_log, banner);
        if (_settings.Created)
        {
            HostLog.SettingsCreated(_log, paths.SettingsFile);
        }

        if (_settings.LoadWarning is { } problem)
        {
            HostLog.SettingsUnreadable(_log, problem);
        }

        _crash = new CrashHandler(_log, paths.Logs);
        GlobalExceptionHooks.Install(_log, _crash.ShowAndExit);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();

        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders().AddSerilog(Log.Logger));
        services.AddSingleton<IAppPaths>(paths);
        services.AddSingleton<ISettingsStore>(_settings);
        services.AddWpfBlazorWebView();
        services.AddFluentUIComponents();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        _services = services.BuildServiceProvider();

        new MainWindow(_services, paths).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var minutes = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMinutes;
        HostLog.Exiting(_log, e.ApplicationExitCode, minutes);
        _services?.Dispose();
        _settings?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>A WPF UI-thread exception is fatal: log it, show the crash dialog, exit (LOGGING.md §8).</summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HostLog.UiThreadException(_log, e.Exception);
        e.Handled = true;
        _crash?.ShowAndExit(e.Exception);
    }
}
