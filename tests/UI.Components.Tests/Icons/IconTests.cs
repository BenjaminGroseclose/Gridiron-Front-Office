using Bunit;
using GridironFrontOffice.UI.Components.Icons;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using FluentIconView = Microsoft.FluentUI.AspNetCore.Components.FluentIcon<Microsoft.FluentUI.AspNetCore.Components.Icon>;

namespace GridironFrontOffice.UI.Components.Tests.Icons;

public sealed class IconTests : GfoTestContext
{
    [Fact]
    public void Every_GFO_icon_name_exists_in_every_size_and_both_styles()
    {
        var missing = new List<string>();
        foreach (var name in Enum.GetValues<IconName>())
        {
            foreach (var size in IconCatalog.Sizes)
            {
                foreach (var filled in new[] { false, true })
                {
                    try
                    {
                        IconCatalog.Create(name, size, filled).ShouldNotBeNull();
                    }
                    catch (TypeLoadException)
                    {
                        missing.Add($"{name} {size} {(filled ? "Filled" : "Regular")}");
                    }
                }
            }
        }

        missing.ShouldBeEmpty("Every IconName must exist in Fluent System Icons at 16, 20 and 24 px, Regular and Filled.");
    }

    [Fact]
    public void Sizes_outside_the_set_are_refused() =>
        Should.Throw<ArgumentOutOfRangeException>(() => IconCatalog.Create(IconName.Home, 18));

    [Fact]
    public void An_icon_without_a_label_is_decorative()
    {
        var cut = Render<Icon>(p => p.Add(i => i.Name, IconName.Home));

        var icon = cut.Find(".gfo-icon");
        icon.GetAttribute("aria-hidden").ShouldBe("true");
        icon.HasAttribute("role").ShouldBeFalse();
    }

    [Theory]
    [InlineData(IconColor.Accent, "gfo-icon-accent")]
    [InlineData(IconColor.Muted, "gfo-icon-muted")]
    [InlineData(IconColor.Faint, "gfo-icon-faint")]
    [InlineData(IconColor.Positive, "gfo-icon-positive")]
    [InlineData(IconColor.Warning, "gfo-icon-warning")]
    [InlineData(IconColor.Negative, "gfo-icon-negative")]
    public void A_color_role_becomes_its_token_class(IconColor color, string cssClass)
    {
        var cut = Render<Icon>(p => p.Add(i => i.Name, IconName.Warning).Add(i => i.Color, color).Add(i => i.Class, "extra"));

        var classes = cut.Find(".gfo-icon").ClassList;
        classes.ShouldContain(cssClass);
        classes.ShouldContain("extra");
    }

    [Theory]
    [InlineData(IconColor.Inherit, "currentColor")]
    [InlineData(IconColor.Accent, "var(--gfo-accent)")]
    [InlineData(IconColor.Muted, "var(--colorNeutralForeground2)")]
    [InlineData(IconColor.Faint, "var(--colorNeutralForeground3)")]
    [InlineData(IconColor.Positive, "var(--gfo-status-positive)")]
    [InlineData(IconColor.Warning, "var(--gfo-status-warning)")]
    [InlineData(IconColor.Negative, "var(--gfo-status-negative)")]
    public void Each_color_role_names_its_token(IconColor color, string token) =>
        color.Token().ShouldBe(token);

    [Fact]
    public void The_catalog_can_hand_a_Fluent_parameter_a_colored_icon()
    {
        // Rendered where Fluent puts it (IconStart, IconRest): the token reaches the page as the fill.
        var plain = Render<FluentIconView>(p => p.Add(i => i.Value, IconCatalog.Create(IconName.Play)));
        var colored = Render<FluentIconView>(p => p.Add(i => i.Value, IconCatalog.Create(IconName.Play, 20, filled: true, color: IconColor.Accent)));

        plain.Find("svg").GetAttribute("style")!.ShouldNotContain("var(", customMessage: "By default the icon draws in the text color around it.");
        colored.Find("svg").GetAttribute("style")!.ShouldContain("var(--gfo-accent)");
    }

    [Fact]
    public void By_default_an_icon_takes_the_text_color_around_it()
    {
        var cut = Render<Icon>(p => p.Add(i => i.Name, IconName.Home));

        cut.Find(".gfo-icon").ClassList.Any(c => c.StartsWith("gfo-icon-", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public void A_labelled_icon_is_an_image_with_that_name()
    {
        var cut = Render<Icon>(p => p.Add(i => i.Name, IconName.Trophy).Add(i => i.Label, "Champions").Add(i => i.Width, 18));

        var icon = cut.Find(".gfo-icon");
        icon.GetAttribute("role").ShouldBe("img");
        icon.HasAttribute("aria-hidden").ShouldBeFalse();
        cut.Markup.ShouldContain("Champions");
        cut.Markup.ShouldContain("18px");
    }
}
