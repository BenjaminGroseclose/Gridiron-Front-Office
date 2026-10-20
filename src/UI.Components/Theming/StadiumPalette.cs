namespace GridironFrontOffice.UI.Components.Theming;

/// <summary>
/// The Stadium neutrals (VISUAL_DESIGN.md §3.1–3.2). This is where the palette's values live; everything else reads
/// them through Fluent's tokens, which <see cref="StadiumTheme"/> sets from here.
/// </summary>
public sealed record StadiumPalette(
    string Ground,
    string Surface1,
    string Surface2,
    string Surface3,
    string Border,
    string BorderStrong,
    string Text,
    string Muted,
    string Faint)
{
    /// <summary>"Slate", the default mode.</summary>
    public static StadiumPalette Dark { get; } = new(
        Ground: "#0F1318", Surface1: "#171C23", Surface2: "#1E242D", Surface3: "#262E38",
        Border: "#2C3540", BorderStrong: "#44505F", Text: "#F1F4F7", Muted: "#BAC3CE", Faint: "#8F9AA8");

    public static StadiumPalette Light { get; } = new(
        Ground: "#EDF0F4", Surface1: "#FFFFFF", Surface2: "#F6F8FA", Surface3: "#E9EDF2",
        Border: "#DDE2E8", BorderStrong: "#B9C1CC", Text: "#0D1117", Muted: "#39424E", Faint: "#5B6573");

    public static StadiumPalette For(bool dark) => dark ? Dark : Light;
}
