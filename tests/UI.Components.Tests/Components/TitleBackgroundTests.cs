using Bunit;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Components.Tests.Components;

public sealed class TitleBackgroundTests : GfoTestContext
{
    [Fact]
    public void It_is_one_decorative_svg_that_fills_its_parent()
    {
        var cut = Render<TitleBackground>(p => p.Add(b => b.Class, "title-bg").AddUnmatched("data-testid", "bg"));

        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain("gfo-title-bg");
        svg.ClassList.ShouldContain("title-bg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
        svg.GetAttribute("viewBox").ShouldBe("0 0 1920 1080");
        svg.GetAttribute("preserveAspectRatio").ShouldBe("xMidYMid slice");
        svg.GetAttribute("data-testid").ShouldBe("bg");
        cut.FindAll("svg").Count.ShouldBe(1);
    }

    [Fact]
    public void The_field_and_the_play_are_all_there()
    {
        var cut = Render<TitleBackground>();

        cut.FindAll(".field-lines path").Count.ShouldBe(9);        // two sidelines and seven yard lines
        cut.FindAll(".hash-marks path").Count.ShouldBe(6);
        cut.FindAll("text").ShouldBeEmpty("No yard numbers: the field is lines only.");
        cut.FindAll("path.los").Count.ShouldBe(1);
        cut.FindAll(".defense path").Count.ShouldBe(11);
        cut.FindAll(".offense circle").Count.ShouldBe(10);
        cut.FindAll(".route path").Count.ShouldBe(4);
    }

    [Fact]
    public void Each_instance_owns_its_arrowhead_marker()
    {
        var first = Render<TitleBackground>();
        var second = Render<TitleBackground>();

        var firstID = first.Instance.MarkerID;
        var secondID = second.Instance.MarkerID;
        firstID.ShouldNotBe(secondID);
        first.Find("marker").GetAttribute("id").ShouldBe(firstID);
        first.Find(".route").GetAttribute("marker-end").ShouldBe($"url(#{firstID})");
        second.Find(".route").GetAttribute("marker-end").ShouldBe($"url(#{secondID})");
        first.Find("marker .route-head").ShouldNotBeNull();
    }
}
