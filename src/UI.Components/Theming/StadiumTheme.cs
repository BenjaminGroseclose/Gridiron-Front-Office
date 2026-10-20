using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Theming;

/// <summary>
/// Sets the Stadium values on the typed Theme that Fluent built (VISUAL_DESIGN.md §9, verified in Spike S0b): fonts,
/// radii, the neutral surfaces and text, and the accent colors GFO computed. Fluent's own exact mode would use the raw
/// team color for text and marks too, which vanishes on a dark surface when the team color is dark.
/// </summary>
internal static class StadiumTheme
{
    public static void Apply(Theme theme, StadiumPalette palette, GfoThemeState accent)
    {
        theme.Fonts.Family.Base = "'Archivo', system-ui, sans-serif";
        theme.Fonts.Family.Numeric = "'Archivo', system-ui, sans-serif";
        theme.Fonts.Family.Monospace = "'JetBrains Mono', ui-monospace, monospace";

        var radius = theme.Borders.Radius;
        radius.Small = "8px";
        radius.Medium = "14px";
        radius.Large = "20px";
        radius.Circular = "999px";

        var n = theme.Colors.Neutral;
        n.Background1 = palette.Surface1;
        n.Background1Hover = palette.Surface2;
        n.Background1Pressed = palette.Surface3;
        n.Background1Selected = palette.Surface2;
        n.Background2 = palette.Surface2;
        n.Background2Hover = palette.Surface3;
        n.Background2Pressed = palette.Surface3;
        n.Background2Selected = palette.Surface3;

        // Ground, and its states one surface step up, so no Fluent control falls back to its own default grays.
        n.Background3 = n.Background4 = n.Background5 = palette.Ground;
        n.Background3Hover = n.Background4Hover = n.Background5Hover = palette.Surface1;
        n.Background3Pressed = n.Background4Pressed = n.Background5Pressed = palette.Surface2;
        n.Background3Selected = n.Background4Selected = n.Background5Selected = palette.Surface1;

        n.CardBackground = palette.Surface1;
        n.CardBackgroundHover = palette.Surface2;
        n.CardBackgroundPressed = palette.Surface3;
        n.CardBackgroundSelected = palette.Surface2;

        n.Foreground1 = n.Foreground1Hover = n.Foreground1Pressed = n.Foreground1Selected = palette.Text;
        n.Foreground2 = palette.Muted;
        n.Foreground2Hover = n.Foreground2Pressed = n.Foreground2Selected = palette.Text;
        n.Foreground3 = palette.Faint;
        n.Foreground3Hover = n.Foreground3Pressed = n.Foreground3Selected = palette.Muted;
        n.Foreground4 = palette.Faint;

        n.Stroke1 = n.Stroke1Hover = n.Stroke1Pressed = n.Stroke1Selected = palette.BorderStrong;
        n.Stroke2 = palette.Border;
        n.Stroke3 = palette.Border;
        n.StrokeSubtle = palette.Border;
        n.ForegroundOnBrand = accent.OnAccent;

        var brand = theme.Colors.Brand;
        brand.Foreground1 = accent.AccentText;
        brand.Foreground2 = accent.AccentText;
        brand.Foreground2Hover = accent.AccentText;
        brand.Foreground2Pressed = accent.AccentText;
        brand.ForegroundLink = accent.AccentText;
        brand.ForegroundLinkHover = accent.AccentText;
        brand.ForegroundLinkPressed = accent.AccentText;
        brand.ForegroundLinkSelected = accent.AccentText;
        brand.Stroke1 = accent.Accent;
        brand.Stroke2 = accent.Accent;

        var compound = theme.Colors.Compound;
        compound.BrandForeground1 = accent.Accent;
        compound.BrandForeground1Hover = accent.Accent;
        compound.BrandForeground1Pressed = accent.Accent;
        compound.BrandStroke = accent.Accent;
        compound.BrandStrokeHover = accent.Accent;
        compound.BrandStrokePressed = accent.Accent;
    }
}
