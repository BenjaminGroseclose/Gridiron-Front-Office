namespace GridironFrontOffice.UI.Components.Theming;

/// <summary>
/// What the player chose in Settings → Appearance, plus the accent color to use (VISUAL_DESIGN.md §2, §3.3, §5). This
/// layer knows nothing about saves or teams: the UI maps the settings and the user team's primary color onto it.
/// </summary>
/// <param name="AccentColor">The exact accent fill as <c>#RRGGBB</c>, or null for Fluent's neutral brand blue.</param>
public sealed record GfoAppearance(
    GfoThemeMode Mode = GfoThemeMode.Dark,
    string? AccentColor = null,
    GfoDensity Density = GfoDensity.Comfortable)
{
    /// <summary>Fluent's default brand blue: the accent before a league is open, and with Accent: Neutral.</summary>
    public const string NeutralAccent = "#0F6CBD";
}

/// <summary>Dark is the default (D-031). Match Windows is resolved by GfoThemeService; Fluent never sees System.</summary>
public enum GfoThemeMode
{
    Dark,
    Light,
    MatchWindows,
}

/// <summary>Table rows of 46 px (Comfortable) or 36 px (Compact).</summary>
public enum GfoDensity
{
    Comfortable,
    Compact,
}

/// <summary>The theme as last applied: the resolved mode and the accent colors GfoThemeService computed.</summary>
/// <param name="AccentFill">The exact accent: primary buttons, hero bands, badges.</param>
/// <param name="OnAccent">Text and icons on the accent fill.</param>
/// <param name="Accent">Marks on the page (≥ 3:1 on Surface 1): icons, selection, focus.</param>
/// <param name="AccentText">Accent-colored text and links (≥ 4.5:1 on Surface 1).</param>
public sealed record GfoThemeState(
    GfoAppearance Appearance,
    bool IsDark,
    string AccentFill,
    string OnAccent,
    string Accent,
    string AccentText)
{
    /// <summary>The grid row height for the density (VISUAL_DESIGN.md §5): <c>DataGrid&lt;T&gt;</c>'s <c>ItemSize</c>.</summary>
    public int RowHeight => Appearance.Density == GfoDensity.Compact ? 36 : 46;
}
