using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Desktop.Windowing;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.UI.Components.Theming;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace GridironFrontOffice.Desktop;

/// <summary>
/// The one window: the web view under the app bar's caption buttons (D-068). It opens where it was last closed, fitted
/// to today's monitors, paints the saved mode's Ground before the page exists, follows the page's theme mode, and saves
/// its placement when it closes.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private const int SwpNoSize = 0x0001;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoActivate = 0x0010;
    private const int SwShowMaximized = 3;

    private readonly ILogger<MainWindow> _log;
    private readonly IAppPaths _paths;
    private readonly ISettingsStore _settings;
    private readonly TitleBar _titleBar;
    private readonly StartupPlacement _startup;
    private readonly Stopwatch _webViewStartup = new();
    private bool _dark;
    private WindowPlacement? _normalBounds;
    private bool _restoreToMaximized;
    private bool _closing;
    private bool _readyToClose;
    private bool _themedOnce;

    /// <param name="dark">The mode for the first paint, from settings.json (the page takes over once it's up).</param>
    public MainWindow(IServiceProvider services, IAppPaths paths, ISettingsStore settings, bool dark)
    {
        _log = services.GetRequiredService<ILogger<MainWindow>>();
        _paths = paths;
        _settings = settings;
        _dark = dark;
        InitializeComponent();

        _titleBar = new TitleBar(this, Root, WebView, CaptionButtons, MaxButton);
        _titleBar.ApplyMode(dark);
        Background = HostColors.Brush(StadiumPalette.For(dark).Ground);

        _startup = WindowPlacementRules.Fit(settings.Current.Window, Monitors.Current());
        Width = _startup.Width;
        Height = _startup.Height;
        if (_startup.Left is not null)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;   // positioned in OnSourceInitialized, in physical pixels
        }

        _restoreToMaximized = _startup.Maximized;
        StateChanged += (_, _) => _restoreToMaximized = WindowState switch
        {
            WindowState.Maximized => true,
            WindowState.Normal => false,
            _ => _restoreToMaximized,   // minimized: restoring goes back to whatever it was
        };
        LocationChanged += (_, _) => TrackNormalBounds();
        SizeChanged += (_, _) => TrackNormalBounds();

        WebView.Services = services;
        WebView.BlazorWebViewInitializing += OnWebViewInitializing;
        WebView.BlazorWebViewInitialized += OnWebViewInitialized;
        ContentRendered += (_, _) => HostLog.WindowShown(_log, ActualWidth, ActualHeight);
#if DEBUG
        // Debug builds: Ctrl+Shift+F12 throws on the WPF UI thread, to prove the DispatcherUnhandledException path.
        PreviewKeyDown += ThrowOnUiThreadForTesting;
#endif
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _titleBar.Attach(_dark);

        var handle = new WindowInteropHelper(this).Handle;
        if (_startup is { Left: { } left, Top: { } top })
        {
            SetWindowPos(handle, IntPtr.Zero, left, top, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
        }

        TrackNormalBounds();

        // Maximized: show it maximized from here, over the normal bounds just set. WindowState = Maximized in the
        // constructor would create the window maximized, so the position above would become its maximized rectangle and
        // Restore would go wherever Windows first put it.
        if (_startup.Maximized)
        {
            ShowWindow(handle, SwShowMaximized);
        }

        var placement = string.Create(CultureInfo.InvariantCulture,
            $"{(_startup.FromSaved ? "saved" : _settings.Current.Window is null ? "default" : "default (the saved one is off every monitor)")}, " +
            $"{_startup.Width:F0}x{_startup.Height:F0} DIP at ({_startup.Left},{_startup.Top}), maximized {_startup.Maximized}");
        HostLog.WindowPlacement(_log, placement);
    }

    /// <summary>Saves the placement first (it's async), then closes for real.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_readyToClose)
        {
            e.Cancel = true;
            if (!_closing)
            {
                _closing = true;
                _ = SavePlacementThenCloseAsync();
            }

            return;
        }

        base.OnClosing(e);
    }

    private async Task SavePlacementThenCloseAsync()
    {
        try
        {
            if (_normalBounds is { } normal)
            {
                var maximized = WindowState == WindowState.Maximized || (WindowState == WindowState.Minimized && _restoreToMaximized);
                await _settings.SaveAsync(_settings.Current with { Window = normal with { Maximized = maximized } });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            HostLog.WindowPlacementNotSaved(_log, ex);
        }
        finally
        {
            _readyToClose = true;
            Close();
        }
    }

    /// <summary>Remembers the window's bounds whenever it's neither maximized nor minimized (physical pixels, with the DPI).</summary>
    private void TrackNormalBounds()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || IsZoomed(handle) || IsIconic(handle) || !GetWindowRect(handle, out var r))
        {
            return;
        }

        _normalBounds = new WindowPlacement(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top, (int)GetDpiForWindow(handle), Maximized: false);
    }

    /// <summary>The page applied a theme: the frame, the caption buttons and the native backgrounds follow its mode.</summary>
    private void ApplyMode(bool dark)
    {
        if (dark == _dark)
        {
            return;
        }

        _dark = dark;
        _titleBar.ApplyMode(dark);
        var ground = StadiumPalette.For(dark).Ground;
        Background = HostColors.Brush(ground);
        WebView.WebView.DefaultBackgroundColor = HostColors.Drawing(ground);
    }

    private void OnWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        // The WebView2 profile lives under the app's data folder, not next to the exe (which an updater replaces).
        e.UserDataFolder = _paths.WebView2;
        // Until the page paints, WebView2 shows this. The host page stays transparent while booting (index.html).
        WebView.WebView.DefaultBackgroundColor = HostColors.Drawing(StadiumPalette.For(_dark).Ground);
        _webViewStartup.Start();
    }

    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var core = e.WebView.CoreWebView2;
        HostLog.WebViewReady(_log, _webViewStartup.ElapsedMilliseconds, core.Environment.BrowserVersionString);

        // The drag regions arrive in CSS px, which equal DIPs only at zoom 1. Density is the app's own setting.
        core.Settings.IsZoomControlEnabled = false;
#if !DEBUG
        // An app, not a browser: no F5 reload, print, find bar or DevTools keys. Editing keys still work.
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
        core.WebMessageReceived += OnPageMessage;
        core.NavigationCompleted += (_, _) =>
        {
            var sinceStart = (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            HostLog.PageLoaded(_log, sinceStart);
#if DEBUG
            RunDiagnosticsTest(core);
#endif
        };
    }

    /// <summary>gfo-host.js's messages. Blazor's own (prefixed, not JSON) share the channel and are skipped.</summary>
    private void OnPageMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string? json;
        try
        {
            json = e.TryGetWebMessageAsString();
        }
        catch (ArgumentException)
        {
            return;   // not a string, so not ours
        }

        switch (PageMessages.Parse(json))
        {
            case DragRegionsMessage drag:
                _titleBar.SetDragRegions(drag.Regions);
                break;
            case ThemeModeMessage theme:
                ApplyMode(theme.Dark);
                LogFirstThemedScreen();
                break;
        }
    }

    /// <summary>
    /// The startup budget's end point (M0 exit: launch to a themed screen under 3 s). The first theme message means the
    /// theme service has applied and lifted the boot hide, so the next frame shows the themed page.
    /// </summary>
    private void LogFirstThemedScreen()
    {
        if (!_themedOnce)
        {
            _themedOnce = true;
            var sinceStart = (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            HostLog.FirstThemedScreen(_log, sinceStart);
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    // Only reached if the HTMAXBUTTON hit test isn't in effect (Windows normally owns this button's clicks).
    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

#if DEBUG
    /// <summary>
    /// Debug builds: <c>--diag-test=page|js|task|crash|ui</c> fires one failure hook after startup, so the logging and
    /// crash paths can be proven without clicking inside the WebView. The first four press the Home page's diagnostics
    /// test panel; <c>ui</c> throws on the WPF UI thread (the same as Ctrl+Shift+F12).
    /// </summary>
    private static void RunDiagnosticsTest(CoreWebView2 core)
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out Monitors.NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);
}
