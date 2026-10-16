using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using GridironFrontOffice.Desktop.Spike;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2.Core;

namespace GridironFrontOffice.Desktop;

internal sealed partial class MainWindow : Window
{
    private readonly SpikeOptions _options;
    private readonly SpikeChrome _chrome;

    public MainWindow(IServiceProvider services, SpikeOptions options)
    {
        _options = options;
        InitializeComponent();
        SpikeProbe.Mark("xaml-loaded");

        var palette = SpikePalette.For(options.Dark);
        palette.ApplyTo(Resources);
        if (options.Fallback)
        {
            Background = (System.Windows.Media.Brush)Resources["Ground"];
        }

        ConfigureTitleBar();
        _chrome = new SpikeChrome(this, options, Root, WebView, CaptionButtons, MaxButton);

        WebView.Services = services;
        WebView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(SpikeRoot) });
        WebView.BlazorWebViewInitializing += OnWebViewInitializing;
        WebView.BlazorWebViewInitialized += OnWebViewInitialized;
        Loaded += (_, _) => SpikeProbe.Mark("window-loaded");

        ContentRendered += (_, _) => SpikeProbe.Mark("window-content-rendered");
        SpikeRoot.Settled += OnSettled;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _chrome.Attach();
        SpikeProbe.Mark("window-source-initialized", _options.ToString());
    }

    private void ConfigureTitleBar()
    {
        switch (_options.TitleBar)
        {
            case TitleBarMode.Chrome:
                CaptionStrip.Visibility = Visibility.Visible;
                CaptionButtons.Visibility = Visibility.Visible;
                break;
            case TitleBarMode.Html:
                // The web view fills the window; the caption buttons float over the HTML top bar's right end (40 px).
                System.Windows.Controls.Grid.SetRow(WebView, 0);
                System.Windows.Controls.Grid.SetRowSpan(WebView, 2);
                CaptionButtons.Visibility = Visibility.Visible;
                foreach (var button in new[] { MinButton, MaxButton, CloseButton })
                {
                    button.Height = 40;
                }

                break;
        }

        foreach (var button in new[] { MinButton, MaxButton, CloseButton })
        {
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
        }
    }

    private void OnWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        if (Environment.GetEnvironmentVariable("GFO_WEBVIEW_DATA") is { Length: > 0 } folder)
        {
            e.UserDataFolder = folder;
        }

        if (_options.Fallback)
        {
            var g = SpikePalette.For(_options.Dark).Ground;
            WebView.WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(g.R, g.G, g.B);
        }

        SpikeProbe.Mark("webview-initializing");
    }

    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var core = e.WebView.CoreWebView2;
        SpikeProbe.Mark("webview-initialized", $"runtime={core.Environment.BrowserVersionString}");
        if (_options.OsLight)
        {
            core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
        }

        core.DOMContentLoaded += (_, _) => SpikeProbe.Mark("dom-content-loaded");
        core.NavigationCompleted += (_, _) => SpikeProbe.Mark("navigation-completed");
    }

    private void OnSettled()
    {
        _chrome.LogMetrics("settled");
        if (Environment.GetEnvironmentVariable("GFO_SCHEME_TEST") == "1")
        {
            RunSchemeTest();
        }
        else if (SpikeProbe.ExitWhenSettled)
        {
            After(TimeSpan.FromMilliseconds(1500), Close);
        }
    }

    /// <summary>Flips WebView2's preferred color scheme the way a Windows light/dark switch would, then back.</summary>
    private void RunSchemeTest()
    {
        var core = WebView.WebView.CoreWebView2;
        var step = 0;
        foreach (var scheme in new[] { CoreWebView2PreferredColorScheme.Light, CoreWebView2PreferredColorScheme.Dark, CoreWebView2PreferredColorScheme.Light, CoreWebView2PreferredColorScheme.Auto })
        {
            After(TimeSpan.FromMilliseconds(800 + (1200 * step++)), () =>
            {
                core.Profile.PreferredColorScheme = scheme;
                SpikeProbe.Mark("scheme-set", scheme.ToString());
                _ = ReportSchemeAsync(core, scheme);
            });
        }

        After(TimeSpan.FromMilliseconds(800 + (1200 * step)), Close);
    }

    private static async Task ReportSchemeAsync(CoreWebView2 core, CoreWebView2PreferredColorScheme scheme)
    {
        await Task.Delay(400);
        var dark = await core.ExecuteScriptAsync("matchMedia('(prefers-color-scheme: dark)').matches");
        SpikeProbe.Mark("scheme-read", $"after {scheme}: page says dark={dark}");
    }

    private static void After(TimeSpan delay, Action action)
    {
        var timer = new DispatcherTimer { Interval = delay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        timer.Start();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    // Only reached if the HTMAXBUTTON hit test isn't in effect (Windows normally owns this button's clicks).
    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
