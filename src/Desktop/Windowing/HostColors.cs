using System.Windows.Media;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>The Stadium palette's hex values (<c>StadiumPalette</c>) as WPF brushes and Win32 colors, for the native frame.</summary>
internal static class HostColors
{
    /// <summary>The palette's <c>#RRGGBB</c>.</summary>
    public static Color Parse(string hex) => Color.FromRgb(
        Convert.ToByte(hex[1..3], 16), Convert.ToByte(hex[3..5], 16), Convert.ToByte(hex[5..7], 16));

    public static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush(Parse(hex));
        brush.Freeze();
        return brush;
    }

    /// <summary>A Win32 COLORREF (0x00BBGGRR).</summary>
    public static int ColorRef(string hex)
    {
        var c = Parse(hex);
        return c.R | (c.G << 8) | (c.B << 16);
    }

    /// <summary>WebView2's background before the page paints (it ignores alpha other than 0 or 255).</summary>
    public static System.Drawing.Color Drawing(string hex)
    {
        var c = Parse(hex);
        return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
    }
}
