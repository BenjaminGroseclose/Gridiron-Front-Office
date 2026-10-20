using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using GridironFrontOffice.UI.Components.Theming;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>
/// The app bar is the title bar (D-068, UI_DESIGN_SYSTEM.md §7). <see cref="WindowChrome"/> turns the whole window into
/// client area but keeps the resize border, Aero Snap, the DWM shadow and rounded corners. A <c>WM_NCHITTEST</c> hook
/// gives the window its title bar back:
/// <list type="number">
/// <item><c>HTMAXBUTTON</c> over the WPF maximize button, so Windows 11 shows Snap Layouts. Windows then owns that
/// button, so the hook also draws its hover and pressed states and handles its click.</item>
/// <item>Nothing over minimize and close, which stay ordinary WPF buttons. They aren't marked
/// <c>IsHitTestVisibleInChrome</c>: with no caption band they don't need it, and it would make WindowChrome answer
/// <c>HTCLIENT</c> over them even in the top resize band.</item>
/// <item><c>HTCAPTION</c> over the empty space of the page's drag regions, which gfo-host.js reports.</item>
/// <item>Nothing in the top resize band, so <see cref="WindowChrome"/> answers <c>HTTOP</c>.</item>
/// </list>
/// It also pads a maximized window by its measured overhang and keeps the frame and caption buttons in the theme's mode.
/// </summary>
internal sealed class TitleBar
{
    private const int WmNcHitTest = 0x0084;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcLButtonDblClk = 0x00A3;
    private const int WmNcMouseLeave = 0x02A2;
    private const int HtCaption = 2;
    private const int HtMaxButton = 9;
    private const int TmeLeave = 0x2;
    private const int TmeNonClient = 0x10;
    private const int MonitorDefaultToNearest = 2;

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpRound = 2;

    private const string MaximizeGlyph = "";
    private const string RestoreGlyph = "";
    private const string Hover = "hover";
    private const string Pressed = "pressed";

    private readonly Window _window;
    private readonly FrameworkElement _root;
    private readonly FrameworkElement _webView;
    private readonly FrameworkElement _captionButtons;
    private readonly ContentControl _maxButton;
    private IReadOnlyList<DragRegion> _dragRegions = [];
    private IntPtr _hwnd;

    public TitleBar(Window window, FrameworkElement root, FrameworkElement webView, FrameworkElement captionButtons, ContentControl maxButton)
    {
        (_window, _root, _webView, _captionButtons, _maxButton) = (window, root, webView, captionButtons, maxButton);

        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 0,                                                  // no caption band: the hook decides
            ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness,
            GlassFrameThickness = new Thickness(0, 0, 0, 1),                    // a non-zero DWM frame keeps the shadow
            UseAeroCaptionButtons = false,
        });

        window.StateChanged += (_, _) => OnStateChanged();
        window.SizeChanged += (_, _) => PadForMaximized();
        window.DpiChanged += (_, _) => PadForMaximized();
    }

    /// <summary>Call from <see cref="Window.OnSourceInitialized"/>, after base, so this hook runs before WindowChrome's.</summary>
    public void Attach(bool dark)
    {
        _hwnd = new WindowInteropHelper(_window).Handle;
        HwndSource.FromHwnd(_hwnd)!.AddHook(WndProc);
        SetDwm(DwmwaWindowCornerPreference, DwmwcpRound);
        ApplyMode(dark);
    }

    /// <summary>The page's drag regions, in CSS px relative to the web view.</summary>
    public void SetDragRegions(IReadOnlyList<DragRegion> regions) => _dragRegions = regions;

    /// <summary>The frame and the caption buttons in the theme's mode (Windows 11 ignores unknown DWM attributes on 10).</summary>
    public void ApplyMode(bool dark)
    {
        var palette = StadiumPalette.For(dark);
        _window.Resources["GfoCaptionForeground"] = HostColors.Brush(palette.Text);
        _window.Resources["GfoCaptionInactive"] = HostColors.Brush(palette.Faint);
        _window.Resources["GfoCaptionHover"] = HostColors.Brush(palette.Surface2);
        _window.Resources["GfoCaptionPressed"] = HostColors.Brush(palette.Surface3);
        if (_hwnd != IntPtr.Zero)
        {
            SetDwm(DwmwaUseImmersiveDarkMode, dark ? 1 : 0);
            SetDwm(DwmwaBorderColor, HostColors.ColorRef(palette.Border));
        }
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
                SetMaxButtonState(wParam == HtMaxButton ? (Equals(_maxButton.Tag, Pressed) ? Pressed : Hover) : null);
                TrackNonClientLeave();
                break;
            case WmNcMouseLeave:
                SetMaxButtonState(null);
                break;
            case WmNcLButtonDown or WmNcLButtonDblClk when wParam == HtMaxButton:
                SetMaxButtonState(Pressed);
                handled = true;
                return IntPtr.Zero;
            case WmNcLButtonUp when wParam == HtMaxButton:
                if (Equals(_maxButton.Tag, Pressed))
                {
                    _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }

                SetMaxButtonState(Hover);
                handled = true;
                return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private int HitTest(IntPtr lParam)
    {
        var screen = new Point((short)(lParam.ToInt64() & 0xFFFF), (short)((lParam.ToInt64() >> 16) & 0xFFFF));

        // Leave the top resize band to WindowChrome (HTTOP), as Windows does for real title bars.
        if (_window.WindowState != WindowState.Maximized && _window.PointFromScreen(screen).Y < SystemParameters.WindowResizeBorderThickness.Top)
        {
            return 0;
        }

        if (Contains(_maxButton, screen))
        {
            return HtMaxButton;
        }

        if (Contains(_captionButtons, screen))
        {
            return 0;
        }

        if (_webView.IsVisible)
        {
            var p = _webView.PointFromScreen(screen);
            if (_dragRegions.Any(r => r.IsCaption(p.X, p.Y)))
            {
                return HtCaption;
            }
        }

        return 0;
    }

    private static bool Contains(FrameworkElement element, Point screen) =>
        element.IsVisible && new Rect(element.RenderSize).Contains(element.PointFromScreen(screen));

    private void SetMaxButtonState(string? state)
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
        var maximized = _window.WindowState == WindowState.Maximized;
        _maxButton.Content = maximized ? RestoreGlyph : MaximizeGlyph;
        AutomationProperties.SetName(_maxButton, maximized ? "Restore" : "Maximize");
        PadForMaximized();
    }

    /// <summary>
    /// A maximized WindowChrome window hangs past the monitor's work area by its frame, so the content is padded back
    /// in. Measured rather than assumed: <c>WindowResizeBorderThickness</c> (4 px) misses the padded border; it's 8 px
    /// at 100% scale (Spike S0c).
    /// </summary>
    private void PadForMaximized()
    {
        if (_hwnd == IntPtr.Zero || _window.WindowState != WindowState.Maximized)
        {
            _root.Margin = default;
            return;
        }

        var monitor = new Monitors.MonitorInfo { Size = Marshal.SizeOf<Monitors.MonitorInfo>() };
        if (!Monitors.GetMonitorInfo(MonitorFromWindow(_hwnd, MonitorDefaultToNearest), ref monitor) || !GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        var scale = VisualTreeHelper.GetDpi(_window).DpiScaleX;
        _root.Margin = new Thickness(
            Math.Max(0, monitor.Work.Left - window.Left) / scale,
            Math.Max(0, monitor.Work.Top - window.Top) / scale,
            Math.Max(0, window.Right - monitor.Work.Right) / scale,
            Math.Max(0, window.Bottom - monitor.Work.Bottom) / scale);
    }

    private void SetDwm(int attribute, int value) => _ = DwmSetWindowAttribute(_hwnd, attribute, ref value, sizeof(int));

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventInfo
    {
        public int Size;
        public int Flags;
        public IntPtr Hwnd;
        public int HoverTime;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackMouseEvent(ref TrackMouseEventInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out Monitors.NativeRect rect);
}
