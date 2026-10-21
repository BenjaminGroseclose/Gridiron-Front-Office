namespace GridironFrontOffice.UI.Components.Icons;

/// <summary>
/// The color roles an <see cref="Icon"/> can take (VISUAL_DESIGN.md §3.3–3.4), each a theme token applied through a
/// <c>gfo-icon-*</c> class in gfo-tokens.css. The default, <see cref="Inherit"/>, is the text color around the icon.
/// Color is never the only signal: a status-colored icon sits next to the text that says the same thing.
/// </summary>
public enum IconColor
{
    /// <summary>The surrounding text color (<c>currentColor</c>).</summary>
    Inherit,

    /// <summary>The team accent, as a mark (≥ 3:1 on Surface 1).</summary>
    Accent,

    /// <summary>Secondary text.</summary>
    Muted,

    /// <summary>Tertiary text.</summary>
    Faint,

    /// <summary>Good news: healthy, done, available.</summary>
    Positive,

    /// <summary>Needs attention: questionable, expiring, an upgrade.</summary>
    Warning,

    /// <summary>Bad news: injured, failed, over the cap.</summary>
    Negative,
}
