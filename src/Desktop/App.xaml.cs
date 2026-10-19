using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Desktop.Diagnostics;
using GridironFrontOffice.Desktop.Platform;
using GridironFrontOffice.Desktop.Windowing;
using GridironFrontOffice.Infrastructure;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.Infrastructure.Settings;
using GridironFrontOffice.UI;
using GridironFrontOffice.UI.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;
using Serilog.Extensions.Logging;

namespace GridironFrontOffice.Desktop;

/// <summary>
/// The composition root (ARCHITECTURE.md §6.1, LOGGING.md §4): the single-instance guard → paths → settings (before DI,
/// so the log level can come from them) → logging → startup banner → failure hooks → services → window.
/// </summary>
internal sealed partial class App : System.Windows.Application
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    private SingleInstance? _instance;
    private ServiceProvider? _services;
    private SettingsStore? _settings;
    private Microsoft.Extensions.Logging.ILogger _log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    private CrashHandler? _crash;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Before anything touches the logs or settings: a second launch hands over to the running window and leaves.
        _instance = SingleInstance.TryAcquire(e.Args);
        if (_instance is null)
        {
            Shutdown();
            return;
        }

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
        services.AddGfoInfrastructure(paths, app);   // paths, the save manager and the data layer (DATA_ACCESS_GUIDE.md §3.6)
        services.AddSingleton<IPlatformServices, WindowsPlatformServices>();   // the Recycle Bin and Explorer
        services.AddSingleton<ISettingsStore>(_settings);
        services.AddWpfBlazorWebView();
        services.AddFluentUIComponents();
        services.AddSingleton(new ShellOptions(AppBarIsTitleBar: true));   // the app bar is the title bar (D-068)
        services.AddGfoUi();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        _services = services.BuildServiceProvider();

        new MainWindow(_services, paths, _settings, WindowsTheme.StartsDark(_settings.Current.Appearance.Theme)).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instance is null)
        {
            base.OnExit(e);   // the second launch: nothing was started
            return;
        }

        var minutes = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMinutes;
        HostLog.Exiting(_log, e.ApplicationExitCode, minutes);
        _services?.Dispose();
        _settings?.Dispose();
        Log.CloseAndFlush();
        _instance.Dispose();
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
