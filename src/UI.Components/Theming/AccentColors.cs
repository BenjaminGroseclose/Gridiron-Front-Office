using System.Globalization;

namespace GridironFrontOffice.UI.Components.Theming;

/// <summary>
/// VISUAL_DESIGN.md §3.3: the colors derived from a team's primary. The fill stays the exact primary. Everything drawn
/// on the page with the accent is lightened (Dark) or darkened (Light) until it contrasts with Surface 1: 3:1 for marks,
/// 4.5:1 for text. Verified on seven team colors in both modes in Spike S0b (D-067).
/// </summary>
public static class AccentColors
{
    private const string White = "#FFFFFF";
    private const string NearBlack = "#0B0D12";

    /// <summary>The derived set for <paramref name="primary"/> on <paramref name="surface1"/>.</summary>
    public static (string Fill, string OnAccent, string Accent, string AccentText) For(string primary, string surface1, bool dark)
    {
        var fill = Normalize(primary);
        return (fill, OnAccent(fill), Corrected(fill, surface1, dark, 3.0), Corrected(fill, surface1, dark, 4.5));
    }

    /// <summary>White or near-black, whichever contrasts more with the fill.</summary>
    public static string OnAccent(string fill) =>
        Contrast(fill, White) >= Contrast(fill, NearBlack) ? White : NearBlack;

    /// <summary>Mixes the color toward white (Dark) or black (Light) in 2% steps until it meets the ratio on the surface.</summary>
    public static string Corrected(string color, string surface, bool dark, double ratio)
    {
        var toward = dark ? White : "#000000";
        for (var step = 0; step <= 50; step++)
        {
            var mixed = Mix(color, toward, step / 50.0);
            if (Contrast(mixed, surface) >= ratio)
            {
                return mixed;
            }
        }

        return toward;
    }

    /// <summary>The WCAG 2 contrast ratio of two colors (1–21).</summary>
    public static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>
    /// <c>#RRGGBB</c> in upper case. Accepts the short <c>#RGB</c> form too. Throws for anything else: a bad team color
    /// is a data error, not something to paint around.
    /// </summary>
    public static string Normalize(string color)
    {
        ArgumentNullException.ThrowIfNull(color);
        var hex = color.Trim();
        if (hex.Length == 4 && hex[0] == '#')
        {
            hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
        }

        if (hex.Length != 7 || hex[0] != '#' || !hex.Skip(1).All(char.IsAsciiHexDigit))
        {
            throw new FormatException($"'{color}' isn't a #RRGGBB color.");
        }

        return hex.ToUpperInvariant();
    }

    private static string Mix(string a, string b, double t)
    {
        var (ca, cb) = (Channels(a), Channels(b));
        int Blend(int i) => (int)Math.Round(ca[i] + ((cb[i] - ca[i]) * t), MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture, $"#{Blend(0):X2}{Blend(1):X2}{Blend(2):X2}");
    }

    private static double Luminance(string color)
    {
        static double Linear(int channel)
        {
            var c = channel / 255d;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        var c = Channels(color);
        return (0.2126 * Linear(c[0])) + (0.7152 * Linear(c[1])) + (0.0722 * Linear(c[2]));
    }

    private static int[] Channels(string color)
    {
        var hex = Normalize(color);
        return [Parse(hex, 1), Parse(hex, 3), Parse(hex, 5)];
    }

    private static int Parse(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
