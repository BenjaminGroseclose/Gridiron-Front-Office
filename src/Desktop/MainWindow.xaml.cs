using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Infrastructure.Logging;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Desktop;

internal sealed partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _log;
    private readonly IAppPaths _paths;
    private readonly Stopwatch _webViewStartup = new();

    public MainWindow(IServiceProvider services, IAppPaths paths)
    {
        _log = services.GetRequiredService<ILogger<MainWindow>>();
        _paths = paths;
        InitializeComponent();

        WebView.Services = services;
        WebView.BlazorWebViewInitializing += OnWebViewInitializing;
        WebView.BlazorWebViewInitialized += OnWebViewInitialized;
        ContentRendered += (_, _) => HostLog.WindowShown(_log, ActualWidth, ActualHeight);
#if DEBUG
        // Debug builds: Ctrl+Shift+F12 throws on the WPF UI thread, to prove the DispatcherUnhandledException path.
        PreviewKeyDown += ThrowOnUiThreadForTesting;
#endif
    }

    private void OnWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        // The WebView2 profile lives under the app's data folder, not next to the exe (which an updater replaces).
        e.UserDataFolder = _paths.WebView2;
        _webViewStartup.Start();
    }

    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var core = e.WebView.CoreWebView2;
        HostLog.WebViewReady(_log, _webViewStartup.ElapsedMilliseconds, core.Environment.BrowserVersionString);
        core.NavigationCompleted += (_, _) =>
        {
            var sinceStart = (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            HostLog.PageLoaded(_log, sinceStart);
#if DEBUG
            RunDiagnosticsTest(core);
#endif
        };
    }

#if DEBUG
    /// <summary>
    /// Debug builds: <c>--diag-test=page|js|task|crash|ui</c> fires one failure hook after startup, so the logging and
    /// crash paths can be proven without clicking inside the WebView. The first four press the Home page's diagnostics
    /// test panel; <c>ui</c> throws on the WPF UI thread (the same as Ctrl+Shift+F12).
    /// </summary>
    private static void RunDiagnosticsTest(Microsoft.Web.WebView2.Core.CoreWebView2 core)
    {
        var test = Environment.GetCommandLineArgs().Skip(1)
            .FirstOrDefault(a => a.StartsWith("--diag-test=", StringComparison.Ordinal))?["--diag-test=".Length..];
        string? button = test switch
        {
            "page" => "diag-page-error",
            "js" => "diag-js-error",
            "task" => "diag-unobserved-task",
            "crash" => "diag-crash",
            _ => null,
        };

        if (button is not null)
        {
            _ = core.ExecuteScriptAsync(
                "const t = setInterval(() => { const b = document.querySelector('[data-testid=" + button + "]');" +
                " if (b) { clearInterval(t); b.click(); } }, 100);");
        }
        else if (test == "ui")
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                throw new InvalidOperationException("Test UI-thread exception (--diag-test=ui)");
            };
            timer.Start();
        }
    }

    private static void ThrowOnUiThreadForTesting(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            throw new InvalidOperationException("Test UI-thread exception (Ctrl+Shift+F12)");
        }
    }
#endif
}
