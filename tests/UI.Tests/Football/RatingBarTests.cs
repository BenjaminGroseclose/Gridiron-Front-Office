using Bunit;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.UI.Football;

namespace GridironFrontOffice.UI.Tests.Football;

/// <summary>
/// M1 WP7 slice 1 (guide §4): an attribute's number and where it sits on 0–99 (VISUAL_DESIGN.md §6), as a meter. The
/// number, the marker and the meter's value are all the shown integer.
/// </summary>
public sealed class RatingBarTests : BunitContext
{
	[Fact]
	public void It_is_a_labelled_meter_from_0_to_99_at_the_shown_value()
	{
		var cut = Render<RatingBar>(p => p.Add(b => b.Value, new PerceivedRating(87.6, 0)).Add(b => b.Label, "Speed"));

		var meter = cut.Find("[role=meter]");
		meter.ClassList.ShouldContain("gfo-rating-bar");
		meter.GetAttribute("aria-valuemin").ShouldBe("0");
		meter.GetAttribute("aria-valuemax").ShouldBe("99");
		meter.GetAttribute("aria-valuenow").ShouldBe("88");
		meter.GetAttribute("aria-label").ShouldBe("Speed");
	}

	[Theory]
	[InlineData(0.0, "0", "poor")]
	[InlineData(59.5, "60", "below")]
	[InlineData(74.0, "74", "avg")]
	[InlineData(89.5, "90", "elite")]
	[InlineData(100.0, "99", "elite")]
	public void The_number_marker_and_meter_all_use_the_shown_value(double estimate, string shown, string tier)
	{
		var cut = Render<RatingBar>(p => p.Add(b => b.Value, new PerceivedRating(estimate, 0)));

		var value = cut.Find(".value");
		value.TextContent.ShouldBe(shown);
		value.ClassList.ShouldContain($"gfo-tier-{tier}");
		cut.Find(".marker").GetAttribute("style").ShouldBe($"--at: {shown}");
		cut.Find("[role=meter]").GetAttribute("aria-valuenow").ShouldBe(shown);
	}

	[Fact]
	public void The_track_is_hidden_from_assistive_technology_and_an_unlabelled_bar_has_no_name()
	{
		var cut = Render<RatingBar>(p => p.Add(b => b.Value, new PerceivedRating(70, 0)));

		cut.Find(".track").GetAttribute("aria-hidden").ShouldBe("true");
		cut.Find(".track .marker").ShouldNotBeNull();
		cut.Find("[role=meter]").HasAttribute("aria-label").ShouldBeFalse();
	}

	[Fact]
	public void It_passes_its_class_and_attributes_through()
	{
		var meter = Render<RatingBar>(p => p
			.Add(b => b.Value, new PerceivedRating(70, 0))
			.Add(b => b.Class, "attr")
			.AddUnmatched("data-testid", "bar")).Find("[role=meter]");

		meter.ClassList.ShouldContain("attr");
		meter.GetAttribute("data-testid").ShouldBe("bar");
	}
}
