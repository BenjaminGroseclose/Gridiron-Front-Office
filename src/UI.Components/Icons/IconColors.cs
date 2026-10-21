namespace GridironFrontOffice.UI.Components.Icons;

/// <summary>
/// The token behind each <see cref="IconColor"/>, for the places that take a color string rather than a class: an
/// icon handed to a Fluent parameter (<c>IconStart</c>, <c>IconRest</c>) through <see cref="IconCatalog.Create"/>.
/// The same tokens as the <c>gfo-icon-*</c> classes in gfo-tokens.css.
/// </summary>
public static class IconColors
{
    /// <summary>The CSS value for the role: a <c>var()</c> of its token, or <c>currentColor</c> for <see cref="IconColor.Inherit"/>.</summary>
    public static string Token(this IconColor color) => color switch
    {
        IconColor.Inherit => "currentColor",
        IconColor.Accent => "var(--gfo-accent)",
        IconColor.Muted => "var(--colorNeutralForeground2)",
        IconColor.Faint => "var(--colorNeutralForeground3)",
        IconColor.Positive => "var(--gfo-status-positive)",
        IconColor.Warning => "var(--gfo-status-warning)",
        IconColor.Negative => "var(--gfo-status-negative)",
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Not an icon color role."),
    };
}
