using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GridironFrontOffice.DevHost.Components.Spike;

/// <summary>
/// Spike S0b: a prototype of GfoThemeService. Fluent v5 builds a theme from a team color + mode
/// (CreateCustomThemeAsync); the Stadium tokens (VISUAL_DESIGN.md §3–5, §9) are then overridden on the typed
/// Theme object before it's applied. Mode and color always travel together (v5 issue #5351).
/// </summary>
internal sealed class SpikeTheme(IThemeService themes, IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<SpikeTheme>? _self;
    private IJSObjectReference? _module;
    private bool _systemDark = true;

    /// <summary>Test colors: Fluent's neutral blue plus F-02 team primaries chosen to stress the ramp (dark, loud, pale).</summary>
    public static readonly IReadOnlyList<(string Name, string Hex)> Teams =
    [
        ("Neutral", "#0F6CBD"),
        ("Continentals (colonial blue)", "#1F3F7A"),
        ("Aviators (safety orange)", "#D9541E"),
        ("Captains (sun gold)", "#E1AD21"),
        ("Excelsiors (crimson)", "#A6192E"),
        ("Sidewinders (turquoise)", "#1F9E94"),
        ("Marshals (near black)", "#1E1E1E"),
    ];

    public string Color { get; private set; } = "#0F6CBD";
    public ThemeMode Mode { get; private set; } = ThemeMode.Dark;
    public bool Stadium { get; private set; } = true;
    public bool Exact { get; private set; } = true;
    public string Status { get; private set; } = "Not applied yet";

    public event Action? Changed;

    public Task ApplyAsync(string color, ThemeMode mode, bool stadium) => ApplyAsync(color, mode, stadium, Exact);

    public async Task ApplyAsync(string color, ThemeMode mode, bool stadium, bool exact)
    {
        (Color, Mode, Stadium, Exact) = (color, mode, stadium, exact);
        await EnsureWatchingAsync();

        // Fluent only ever sees an explicit mode. Handing it ThemeMode.System makes Fluent re-apply its *default* theme
        // when the OS flips, dropping the team color and the Stadium tokens (found in S0b).
        var dark = mode == ThemeMode.Dark || (mode == ThemeMode.System && _systemDark);
        var effective = dark ? ThemeMode.Dark : ThemeMode.Light;
        var settings = new ThemeSettings { Color = color, Mode = effective, IsExact = exact };

        if (stadium)
        {
            var theme = await themes.CreateCustomThemeAsync(settings);
            var palette = dark ? Palette.Slate : Palette.Light;
            ApplyStadium(theme, palette);
            if (exact)
            {
                ApplyAccent(theme, color, palette.Surface1, dark);
            }

            await themes.SetThemeAsync(theme);
        }
        else
        {
            await themes.SetThemeAsync(settings);
        }

        var applied = await themes.GetThemeSettingsAsync();
        Status = $"Applied {color} in {mode} mode (accent {Accent ?? "-"}, accent text {AccentText ?? "-"}). Fluent now reports color {applied?.Color ?? "?"}, mode {applied?.Mode.ToString() ?? "?"}, dark = {await themes.IsDarkModeAsync()}.";
        Changed?.Invoke();
    }

    [JSInvokable("OnSystemSchemeChanged")]
    public async Task OnSystemSchemeChangedAsync(bool dark)
    {
        _systemDark = dark;
        if (Mode == ThemeMode.System)
        {
            await ApplyAsync(Color, Mode, Stadium, Exact);
        }
    }

    private async Task EnsureWatchingAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", "./spike.js");
        _self = DotNetObjectReference.Create(this);
        _systemDark = await _module.InvokeAsync<bool>("watchColorScheme", _self);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _self?.Dispose();
    }

    /// <summary>
    /// VISUAL_DESIGN.md §3.3. The fill stays the exact team primary; everything drawn *on the page* with the accent is
    /// lightened (dark) or darkened (light) until it contrasts with Surface 1: 3:1 for marks, 4.5:1 for text.
    /// </summary>
    private void ApplyAccent(Theme theme, string primary, string surface1, bool dark)
    {
        var accent = Corrected(primary, surface1, dark, 3.0);
        var accentText = Corrected(primary, surface1, dark, 4.5);
        Accent = accent;
        AccentText = accentText;

        theme.Colors.Neutral.ForegroundOnBrand = OnAccent(primary);

        var b = theme.Colors.Brand;
        b.Foreground1 = accentText;
        b.Foreground2 = accentText;
        b.Foreground2Hover = accentText;
        b.Foreground2Pressed = accentText;
        b.ForegroundLink = accentText;
        b.ForegroundLinkHover = accentText;
        b.ForegroundLinkPressed = accentText;
        b.ForegroundLinkSelected = accentText;
        b.Stroke1 = accent;
        b.Stroke2 = accent;

        var c = theme.Colors.Compound;
        c.BrandForeground1 = accent;
        c.BrandForeground1Hover = accent;
        c.BrandForeground1Pressed = accent;
        c.BrandStroke = accent;
        c.BrandStrokeHover = accent;
        c.BrandStrokePressed = accent;
    }

    public string? Accent { get; private set; }
    public string? AccentText { get; private set; }

    /// <summary>Mix the color toward white (dark mode) or black (light mode) in 2% steps until it meets the ratio on the surface.</summary>
    public static string Corrected(string hex, string surface, bool dark, double ratio)
    {
        for (var t = 0.0; t <= 1.0; t += 0.02)
        {
            var mixed = Mix(hex, dark ? "#FFFFFF" : "#000000", t);
            if (Contrast(mixed, surface) >= ratio)
            {
                return mixed;
            }
        }

        return dark ? "#FFFFFF" : "#000000";
    }

    private static string Mix(string a, string b, double t)
    {
        int Ch(string hex, int i) => Convert.ToInt32(hex.Substring(1 + (i * 2), 2), 16);
        int M(int i) => (int)Math.Round(Ch(a, i) + ((Ch(b, i) - Ch(a, i)) * t));
        return $"#{M(0):X2}{M(1):X2}{M(2):X2}";
    }

    /// <summary>White or near-black, whichever has the higher WCAG contrast ratio against the fill.</summary>
    public static string OnAccent(string fillHex) =>
        Contrast(fillHex, "#FFFFFF") >= Contrast(fillHex, "#0B0D12") ? "#FFFFFF" : "#0B0D12";

    public static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        static double Channel(string hex, int i)
        {
            var c = Convert.ToInt32(hex.Substring(1 + (i * 2), 2), 16) / 255d;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(hex, 0)) + (0.7152 * Channel(hex, 1)) + (0.0722 * Channel(hex, 2));
    }

    private static void ApplyStadium(Theme theme, Palette p)
    {
        theme.Fonts.Family.Base = "'Archivo', system-ui, sans-serif";
        theme.Fonts.Family.Numeric = "'Archivo', system-ui, sans-serif";
        theme.Fonts.Family.Monospace = "'JetBrains Mono', ui-monospace, monospace";

        theme.Borders.Radius.Small = "8px";
        theme.Borders.Radius.Medium = "14px";
        theme.Borders.Radius.Large = "20px";
        theme.Borders.Radius.XLarge = "20px";
        theme.Borders.Radius.Circular = "999px";

        var n = theme.Colors.Neutral;
        n.Background1 = p.Surface1;
        n.Background1Hover = p.Surface2;
        n.Background1Pressed = p.Surface3;
        n.Background1Selected = p.Surface2;
        n.Background2 = p.Surface2;
        n.Background3 = p.Ground;
        n.Background4 = p.Ground;
        n.Background5 = p.Ground;
        n.CardBackground = p.Surface1;
        n.Foreground1 = p.Text;
        n.Foreground2 = p.Muted;
        n.Foreground3 = p.Faint;
        n.Foreground4 = p.Faint;
        n.Stroke1 = p.BorderStrong;
        n.Stroke2 = p.Border;
        n.Stroke3 = p.Border;
        n.StrokeSubtle = p.Border;
    }

    private sealed record Palette(string Ground, string Surface1, string Surface2, string Surface3,
        string Border, string BorderStrong, string Text, string Muted, string Faint)
    {
        public static readonly Palette Slate = new("#0F1318", "#171C23", "#1E242D", "#262E38", "#2C3540", "#44505F", "#F1F4F7", "#BAC3CE", "#8F9AA8");
        public static readonly Palette Light = new("#EDF0F4", "#FFFFFF", "#F6F8FA", "#E9EDF2", "#DDE2E8", "#B9C1CC", "#0D1117", "#39424E", "#5B6573");
    }
}
