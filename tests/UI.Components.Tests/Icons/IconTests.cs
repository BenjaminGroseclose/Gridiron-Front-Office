using Bunit;
using GridironFrontOffice.UI.Components.Icons;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;

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
