using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.UI.Components.Theming;

namespace GridironFrontOffice.UI.Shell;

/// <summary>Settings → Appearance, plus the user team's primary color, as the design system's appearance.</summary>
public static class AppearanceMapping
{
    /// <param name="teamPrimary">The user team's primary color, or null when no league is open (until M1, always).</param>
    public static GfoAppearance From(AppearanceSettings settings, string? teamPrimary)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new GfoAppearance(
            Mode: settings.Theme switch
            {
                ThemeChoice.Light => GfoThemeMode.Light,
                ThemeChoice.MatchWindows => GfoThemeMode.MatchWindows,
                _ => GfoThemeMode.Dark,
            },
            AccentColor: settings.Accent == AccentMode.Team ? teamPrimary : null,
            Density: settings.Density == Density.Compact ? GfoDensity.Compact : GfoDensity.Comfortable);
    }
}
