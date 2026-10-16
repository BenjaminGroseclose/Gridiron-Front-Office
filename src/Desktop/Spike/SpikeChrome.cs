using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace GridironFrontOffice.Desktop.Spike;

/// <summary>
/// Spike S0c: the custom title bar plumbing for options (a) and (b).
/// <list type="bullet">
/// <item>WindowChrome turns the whole window into client area but keeps the resize border, Aero Snap and the DWM shadow.</item>
/// <item>A WM_NCHITTEST hook answers HTMAXBUTTON over the WPF maximize button, so Windows 11 shows Snap Layouts on hover,
/// and (option b) HTCAPTION over the HTML top bar's empty space, from rectangles the page reports.</item>
/// <item>DWM attributes give the frame dark mode, a Stadium border color and (native mode) a Ground caption.</item>
/// </list>
/// </summary>
internal sealed class SpikeChrome
{
    private const int WmNcHitTest = 0x0084;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcMouseLeave = 0x02A2;
    private const int HtCaption = 2;
    private const int HtMaxButton = 9;
    private const int TmeLeave = 0x2;
    private const int TmeNonClient = 0x10;

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwcpRound = 2;

    private readonly Window _window;
    private readonly SpikeOptions _options;
    private readonly FrameworkElement _root;
    private readonly FrameworkElement _webView;
    private readonly FrameworkElement _maxButton;
    private readonly FrameworkElement _captionButtons;
    private (Rect Bar, Rect[] Holes) _drag = (Rect.Empty, []);
    private IntPtr _hwnd;

    public SpikeChrome(Window window, SpikeOptions options, FrameworkElement root, FrameworkElement webView, FrameworkElement captionButtons, FrameworkElement maxButton)
    {
        (_window, _options, _root, _webView, _captionButtons, _maxButton) = (window, options, root, webView, captionButtons, maxButton);
        Current = this;

        if (options.TitleBar != TitleBarMode.Native)
        {
            WindowChrome.SetWindowChrome(window, new WindowChrome
            {
                // Option (a): the 32 px WPF strip is the caption. Option (b): no caption band; the hook decides from the page.
                CaptionHeight = options.TitleBar == TitleBarMode.Chrome ? 32 : 0,
                ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness,
                GlassFrameThickness = new Thickness(0, 0, 0, 1),   // a non-zero DWM frame keeps the shadow
                UseAeroCaptionButtons = false,
            });
            window.StateChanged += (_, _) => OnStateChanged();
            root.SizeChanged += (_, _) => LogMetrics(window.WindowState.ToString());
        }
    }

    /// <summary>The live instance, so the page's JS interop can hand over drag regions.</summary>
    public static SpikeChrome? Current { get; private set; }

    public IntPtr Handle => _hwnd;

    /// <summary>Call from <see cref="Window.OnSourceInitialized"/>, after base, so this hook runs before WindowChrome's.</summary>
    public void Attach()
    {
        _hwnd = new WindowInteropHelper(_window).Handle;
        SetDwm(DwmwaUseImmersiveDarkMode, _options.Dark ? 1 : 0);
        SetDwm(DwmwaWindowCornerPreference, DwmwcpRound);
        SetDwm(DwmwaBorderColor, ColorRef(SpikePalette.For(_options.Dark).Border));
        if (_options.TitleBar == TitleBarMode.Native)
        {
            SetDwm(DwmwaCaptionColor, ColorRef(SpikePalette.For(_options.Dark).Ground));
            SetDwm(DwmwaTextColor, ColorRef(SpikePalette.For(_options.Dark).Muted));
        }
        else
        {
            HwndSource.FromHwnd(_hwnd)!.AddHook(WndProc);
        }

        LogMetrics("attached");
    }

    /// <summary>Option (b): the top bar's rectangle and its interactive holes, in CSS px (= DIPs at zoom 1).</summary>
    public void SetDragRegions(Rect bar, Rect[] holes, double devicePixelRatio)
    {
        _drag = (bar, holes);
        SpikeProbe.Mark("drag-regions", $"bar={bar} holes={holes.Length} dpr={devicePixelRatio}");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WmNcHitTest:
                var hit = HitTest(lParam);
                if (hit != 0)
                {
                    handled = true;
                    return hit;
                }

                break;
            case WmNcMouseMove:
                SetMaxState(wParam == HtMaxButton ? "hover" : null);
                TrackNonClientLeave();
                break;
            case WmNcMouseLeave:
                SetMaxState(null);
                break;
            case WmNcLButtonDown when wParam == HtMaxButton:
                SetMaxState("pressed");
                handled = true;
                return IntPtr.Zero;
            case WmNcLButtonUp when wParam == HtMaxButton:
                SetMaxState(null);
                _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                handled = true;
                return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private int HitTest(IntPtr lParam)
    {
        var screen = new Point((short)(lParam.ToInt64() & 0xFFFF), (short)((lParam.ToInt64() >> 16) & 0xFFFF));

        // Leave the top resize band to WindowChrome (HTTOP), as Windows does for real title bars.
        var inWindow = _window.PointFromScreen(screen);
        if (_window.WindowState != WindowState.Maximized && inWindow.Y < SystemParameters.WindowResizeBorderThickness.Top)
        {
            return 0;
        }

        if (_maxButton.IsVisible && new Rect(_maxButton.RenderSize).Contains(_maxButton.PointFromScreen(screen)))
        {
            return HtMaxButton;
        }

        // The other caption buttons float over the HTML bar in option (b): WPF (WindowChrome) owns them, not the drag region.
        if (_captionButtons.IsVisible && new Rect(_captionButtons.RenderSize).Contains(_captionButtons.PointFromScreen(screen)))
        {
            return 0;
        }

        if (_options.TitleBar == TitleBarMode.Html)
        {
            var p = _webView.PointFromScreen(screen);
            if (_drag.Bar.Contains(p) && !_drag.Holes.Any(h => h.Contains(p)))
            {
                return HtCaption;
            }
        }

        return 0;
    }

    private void SetMaxState(string? state)
    {
        if (!Equals(_maxButton.Tag, state))
        {
            _maxButton.Tag = state;
        }
    }

    private void TrackNonClientLeave()
    {
        var info = new TrackMouseEventInfo { Size = Marshal.SizeOf<TrackMouseEventInfo>(), Flags = TmeLeave | TmeNonClient, Hwnd = _hwnd };
        TrackMouseEvent(ref info);
    }

    private void OnStateChanged()
    {
        // WindowChrome windows overhang the monitor by the resize frame when maximized. Pad the content back in.
        _root.Margin = _window.WindowState == WindowState.Maximized ? MaximizedOverhang() : default;
        _maxButton.SetValue(System.Windows.Controls.ContentControl.ContentProperty, _window.WindowState == WindowState.Maximized ? "" : "");
    }

    /// <summary>
    /// How far the maximized window hangs past the monitor's work area, in DIPs. Measured rather than assumed:
    /// WindowResizeBorderThickness alone (4 px) misses the padded border, so it was 8 px at 100% scale.
    /// </summary>
    private Thickness MaximizedOverhang()
    {
        var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(MonitorFromWindow(_hwnd, 2), ref monitor);
        GetWindowRect(_hwnd, out var window);
        var scale = VisualTreeHelper.GetDpi(_window).DpiScaleX;
        return new Thickness(
            Math.Max(0, monitor.Work.Left - window.Left) / scale,
            Math.Max(0, monitor.Work.Top - window.Top) / scale,
            Math.Max(0, window.Right - monitor.Work.Right) / scale,
            Math.Max(0, window.Bottom - monitor.Work.Bottom) / scale);
    }

    /// <summary>Logs the content's screen rectangle next to the monitor's work area, DPI and DPI awareness.</summary>
    public void LogMetrics(string when)
    {
        if (!SpikeProbe.Enabled || _hwnd == IntPtr.Zero)
        {
            return;
        }

        var topLeft = _root.PointToScreen(new Point(0, 0));
        var bottomRight = _root.PointToScreen(new Point(_root.ActualWidth, _root.ActualHeight));
        var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(MonitorFromWindow(_hwnd, 2), ref monitor);
        GetWindowRect(_hwnd, out var window);
        var awareness = GetAwarenessFromDpiAwarenessContext(GetWindowDpiAwarenessContext(_hwnd));
        SpikeProbe.Mark("metrics", $"{when}: content=({topLeft.X:F0},{topLeft.Y:F0})-({bottomRight.X:F0},{bottomRight.Y:F0}) " +
            $"window=({window.Left},{window.Top})-({window.Right},{window.Bottom}) " +
            $"work=({monitor.Work.Left},{monitor.Work.Top})-({monitor.Work.Right},{monitor.Work.Bottom}) " +
            $"dpi={GetDpiForWindow(_hwnd)} awareness={awareness} min={_window.MinWidth}x{_window.MinHeight} size={_window.ActualWidth:F0}x{_window.ActualHeight:F0}");
    }

    private void SetDwm(int attribute, int value)
    {
        var hr = DwmSetWindowAttribute(_hwnd, attribute, ref value, sizeof(int));
        if (hr != 0)
        {
            SpikeProbe.Mark("dwm-failed", $"attribute={attribute} hr=0x{hr:X8}");
        }
    }

    private static int ColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventInfo
    {
        public int Size;
        public int Flags;
        public IntPtr Hwnd;
        public int HoverTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref TrackMouseEventInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
}

/// <summary>Spike S0c: the Stadium values the native side needs (VISUAL_DESIGN.md §3.1–3.2).</summary>
internal sealed record SpikePalette(Color Ground, Color Surface1, Color Surface2, Color Surface3, Color Border, Color Text, Color Muted)
{
    private static readonly SpikePalette Slate = new(
        Rgb(0x0F, 0x13, 0x18), Rgb(0x17, 0x1C, 0x23), Rgb(0x1E, 0x24, 0x2D), Rgb(0x26, 0x2E, 0x38), Rgb(0x2C, 0x35, 0x40), Rgb(0xF1, 0xF4, 0xF7), Rgb(0xBA, 0xC3, 0xCE));

    private static readonly SpikePalette Light = new(
        Rgb(0xED, 0xF0, 0xF4), Rgb(0xFF, 0xFF, 0xFF), Rgb(0xF6, 0xF8, 0xFA), Rgb(0xE9, 0xED, 0xF2), Rgb(0xDD, 0xE2, 0xE8), Rgb(0x0D, 0x11, 0x17), Rgb(0x39, 0x42, 0x4E));

    public static SpikePalette For(bool dark) => dark ? Slate : Light;

    /// <summary>Publishes the palette as brushes for the caption strip and buttons (DynamicResource keys).</summary>
    public void ApplyTo(ResourceDictionary resources)
    {
        resources["Ground"] = new SolidColorBrush(Ground);
        resources["CaptionForeground"] = new SolidColorBrush(Text);
        resources["CaptionMuted"] = new SolidColorBrush(Muted);
        resources["CaptionHover"] = new SolidColorBrush(Surface2);
        resources["CaptionPressed"] = new SolidColorBrush(Surface3);
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
