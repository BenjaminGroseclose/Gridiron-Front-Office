using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.Desktop.Spike;

/// <summary>
/// Spike S0c: just enough of the future GfoThemeService (UI_DESIGN_SYSTEM.md §3.2) to measure when the Stadium
/// theme lands relative to the first paint: exact brand color, explicit mode, Stadium fonts, radii and surfaces.
/// </summary>
internal sealed class SpikeTheme(IThemeService themes)
{
    private const string NeutralBrand = "#0F6CBD";

    public async Task ApplyAsync(bool dark)
    {
        var theme = await themes.CreateCustomThemeAsync(new ThemeSettings
        {
            Color = NeutralBrand,
            Mode = dark ? ThemeMode.Dark : ThemeMode.Light,
            IsExact = true,
        });

        theme.Fonts.Family.Base = "'Archivo', system-ui, sans-serif";
        theme.Fonts.Family.Numeric = "'Archivo', system-ui, sans-serif";
        theme.Borders.Radius.Small = "8px";
        theme.Borders.Radius.Medium = "14px";
        theme.Borders.Radius.Large = "20px";
        theme.Borders.Radius.Circular = "999px";

        var (ground, s1, s2, s3, border, text, muted) = dark
            ? ("#0F1318", "#171C23", "#1E242D", "#262E38", "#2C3540", "#F1F4F7", "#BAC3CE")
            : ("#EDF0F4", "#FFFFFF", "#F6F8FA", "#E9EDF2", "#DDE2E8", "#0D1117", "#39424E");
        var n = theme.Colors.Neutral;
        (n.Background1, n.Background1Hover, n.Background1Pressed, n.Background2) = (s1, s2, s3, s2);
        (n.Background3, n.Background4, n.Background5, n.CardBackground) = (ground, ground, ground, s1);
        (n.Foreground1, n.Foreground2, n.Stroke1, n.Stroke2, n.StrokeSubtle) = (text, muted, border, border, border);

        await themes.SetThemeAsync(theme);
    }
}
