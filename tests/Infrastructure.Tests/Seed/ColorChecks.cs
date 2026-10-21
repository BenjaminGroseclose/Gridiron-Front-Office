using System.Globalization;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// The color math the guardrail tests need (F-02 §4.3): WCAG contrast and CIEDE2000 distance on <c>#RRGGBB</c>.
/// Test-only (D-078): the UI's AccentColors already corrects contrast at runtime, so production code never needs it.
/// </summary>
internal static class ColorChecks
{
    private const string NearBlack = "#0B0D12";               // the UI's dark on-color (AccentColors)
    private const double TwentyFiveToTheSeventh = 6_103_515_625;

    /// <summary>The WCAG 2 contrast ratio, from 1 (same) to 21 (black on white).</summary>
    public static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>The better of white and near-black text on <paramref name="fill"/>: the on-color the UI picks.</summary>
    public static double OnColorContrast(string fill) => Math.Max(Contrast(fill, "#FFFFFF"), Contrast(fill, NearBlack));

    /// <summary>How different two colors look (CIEDE2000): about 1 is barely visible, 20+ is clearly different.</summary>
    public static double DeltaE2000(string a, string b) => DeltaE2000(ToLab(a), ToLab(b));

    /// <summary>CIEDE2000 on two CIELAB colors (Sharma, Wu and Dalal, 2005). Angles are in degrees.</summary>
    public static double DeltaE2000((double L, double A, double B) x, (double L, double A, double B) y)
    {
        var c1 = Math.Sqrt(x.A * x.A + x.B * x.B);
        var c2 = Math.Sqrt(y.A * y.A + y.B * y.B);
        var cMean7 = Math.Pow((c1 + c2) / 2, 7);
        var g = 0.5 * (1 - Math.Sqrt(cMean7 / (cMean7 + TwentyFiveToTheSeventh)));
        var a1 = (1 + g) * x.A;
        var a2 = (1 + g) * y.A;
        var c1p = Math.Sqrt(a1 * a1 + x.B * x.B);
        var c2p = Math.Sqrt(a2 * a2 + y.B * y.B);
        var h1p = Hue(x.B, a1);
        var h2p = Hue(y.B, a2);
        var neutral = c1p * c2p == 0;                      // a gray has no hue: the formula's special case

        var dLp = y.L - x.L;
        var dCp = c2p - c1p;
        var dhp = neutral ? 0 : h2p - h1p;
        if (dhp > 180)
        {
            dhp -= 360;
        }
        else if (dhp < -180)
        {
            dhp += 360;
        }

        var dHp = 2 * Math.Sqrt(c1p * c2p) * Math.Sin(Radians(dhp / 2));

        var lMean = (x.L + y.L) / 2;
        var cMeanP = (c1p + c2p) / 2;
        var hMean = neutral ? h1p + h2p
            : Math.Abs(h1p - h2p) <= 180 ? (h1p + h2p) / 2
            : (h1p + h2p + (h1p + h2p < 360 ? 360 : -360)) / 2;

        var t = 1 - 0.17 * Math.Cos(Radians(hMean - 30)) + 0.24 * Math.Cos(Radians(2 * hMean))
            + 0.32 * Math.Cos(Radians(3 * hMean + 6)) - 0.20 * Math.Cos(Radians(4 * hMean - 63));
        var dTheta = 30 * Math.Exp(-Math.Pow((hMean - 275) / 25, 2));
        var cMeanP7 = Math.Pow(cMeanP, 7);
        var rc = 2 * Math.Sqrt(cMeanP7 / (cMeanP7 + TwentyFiveToTheSeventh));
        var lOff = (lMean - 50) * (lMean - 50);
        var sl = 1 + 0.015 * lOff / Math.Sqrt(20 + lOff);
        var sc = 1 + 0.045 * cMeanP;
        var sh = 1 + 0.015 * cMeanP * t;
        var rt = -Math.Sin(Radians(2 * dTheta)) * rc;

        var (l, c, h) = (dLp / sl, dCp / sc, dHp / sh);
        return Math.Sqrt(l * l + c * c + h * h + rt * c * h);

        static double Hue(double b, double a) =>
            a == 0 && b == 0 ? 0 : (Math.Atan2(b, a) * 180 / Math.PI + 360) % 360;

        static double Radians(double degrees) => degrees * Math.PI / 180;
    }

    private static double Luminance(string hex)
    {
        var (r, g, b) = Channels(hex);
        return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);

        // 0.03928, as the UI's AccentColors uses, so these checks agree with what the app shows.
        static double Linear(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static (double L, double A, double B) ToLab(string hex)
    {
        var (r, g, b) = Channels(hex);
        (r, g, b) = (Linear(r), Linear(g), Linear(b));
        var x = F((0.4124564 * r + 0.3575761 * g + 0.1804375 * b) / 0.95047);   // sRGB → XYZ, D65 white
        var y = F(0.2126729 * r + 0.7151522 * g + 0.0721750 * b);
        var z = F((0.0193339 * r + 0.1191920 * g + 0.9503041 * b) / 1.08883);
        return (116 * y - 16, 500 * (x - y), 200 * (y - z));

        // 0.04045 here, the sRGB standard's threshold (the published Lab values use it).
        static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        // (6/29)³ = 216/24389 and (6/29)² = 36/841, written as exact fractions.
        static double F(double t) => t > 216.0 / 24389 ? Math.Cbrt(t) : t / (3 * 36.0 / 841) + 4.0 / 29;
    }

    private static (double R, double G, double B) Channels(string hex)
    {
        return (Channel(1), Channel(3), Channel(5));

        double Channel(int start) => int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
    }
}
