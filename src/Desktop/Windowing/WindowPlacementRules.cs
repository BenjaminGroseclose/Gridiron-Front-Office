using GridironFrontOffice.Application.Settings;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>A rectangle in physical screen pixels.</summary>
internal readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;

    /// <summary>The overlapping area in square pixels, 0 when the two don't touch.</summary>
    public long OverlapArea(PixelRect other)
    {
        long width = Math.Min(Right, other.Right) - Math.Max(Left, other.Left);
        long height = Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top);
        return width > 0 && height > 0 ? width * height : 0;
    }
}

/// <summary>A monitor as the window sees it: its work area (without the taskbar) and its effective DPI.</summary>
internal sealed record MonitorArea(PixelRect WorkArea, int Dpi, bool IsPrimary);

/// <summary>
/// Where the window opens: its top-left in physical pixels (null: let WPF center it), its size in DIPs, and whether this
/// is the saved placement (false: the default, because nothing was saved or the saved one is off every monitor).
/// </summary>
internal sealed record StartupPlacement(int? Left, int? Top, double Width, double Height, bool Maximized, bool FromSaved = false);

/// <summary>
/// Fits the saved window placement onto the monitors that exist now, so the window is never lost on a monitor that has
/// been unplugged and never larger than its screen. Plain arithmetic, so it's tested without a desktop.
/// </summary>
internal static class WindowPlacementRules
{
    /// <summary>The first-launch size (DIPs), designed for 1920 × 1080.</summary>
    public const double DefaultWidth = 1440;

    public const double DefaultHeight = 900;

    /// <summary>The minimum window (DIPs, UI_DESIGN_SYSTEM.md §7).</summary>
    public const double MinWidth = 1280;

    public const double MinHeight = 720;

    /// <summary>
    /// The window goes on the monitor it overlaps most, keeps its size in DIPs, shrinks to that monitor's work area if
    /// it's larger, and moves fully inside it. A saved placement that overlaps no monitor falls back to <see cref="Default"/>.
    /// </summary>
    public static StartupPlacement Fit(WindowPlacement? saved, IReadOnlyList<MonitorArea> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (saved is null || saved.Width <= 0 || saved.Height <= 0 || saved.Dpi <= 0)
        {
            return Default(monitors);
        }

        var savedRect = new PixelRect(saved.Left, saved.Top, saved.Width, saved.Height);
        var monitor = monitors.MaxBy(m => m.WorkArea.OverlapArea(savedRect));
        if (monitor is null || monitor.WorkArea.OverlapArea(savedRect) == 0)
        {
            return Default(monitors);
        }

        // The same size in DIPs on this monitor, within its work area but never below the minimum.
        var work = monitor.WorkArea;
        var scale = monitor.Dpi / 96.0;
        var width = Clamp(saved.Width * (double)monitor.Dpi / saved.Dpi, MinWidth * scale, work.Width);
        var height = Clamp(saved.Height * (double)monitor.Dpi / saved.Dpi, MinHeight * scale, work.Height);
        var left = (int)Clamp(saved.Left, work.Left, work.Right - width);
        var top = (int)Clamp(saved.Top, work.Top, work.Bottom - height);
        return new StartupPlacement(left, top, width / scale, height / scale, saved.Maximized, FromSaved: true);
    }

    /// <summary>
    /// First launch, or nothing usable saved: the default size centered on the primary monitor, or maximized there when
    /// it doesn't fit. With no monitor information at all, WPF centers it.
    /// </summary>
    public static StartupPlacement Default(IReadOnlyList<MonitorArea> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? (monitors.Count > 0 ? monitors[0] : null);
        if (primary is null)
        {
            return new StartupPlacement(Left: null, Top: null, DefaultWidth, DefaultHeight, Maximized: false);
        }

        var work = primary.WorkArea;
        var scale = primary.Dpi / 96.0;
        var width = DefaultWidth * scale;
        var height = DefaultHeight * scale;
        var fits = width <= work.Width && height <= work.Height;
        var left = fits ? work.Left + (int)((work.Width - width) / 2) : work.Left;
        var top = fits ? work.Top + (int)((work.Height - height) / 2) : work.Top;
        return new StartupPlacement(left, top, DefaultWidth, DefaultHeight, Maximized: !fits);
    }

    /// <summary><paramref name="min"/> wins over <paramref name="max"/> when they cross (a screen smaller than the minimum).</summary>
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
