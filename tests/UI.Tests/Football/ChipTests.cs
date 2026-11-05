using Bunit;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.UI.Football;

namespace GridironFrontOffice.UI.Tests.Football;

/// <summary>
/// M1 WP7 slice 1 (guide §4): a rating as a pill. The number is OverallCalculator.Display's, and the tier is that
/// number's, so the color and the number never disagree (89.5 shows 90, in Elite's color).
/// </summary>
public sealed class RatingChipTests : BunitContext
{
	[Theory]
	[InlineData(78.4, "78", "avg")]
	[InlineData(59.4, "59", "poor")]
	[InlineData(59.5, "60", "below")]      // rounded half away from zero, then tiered
	[InlineData(69.5, "70", "avg")]
	[InlineData(79.5, "80", "good")]
	[InlineData(89.49, "89", "good")]
	[InlineData(89.5, "90", "elite")]
	[InlineData(99.6, "99", "elite")]      // capped at 99
	[InlineData(-0.4, "0", "poor")]
	public void It_shows_the_displayed_number_in_that_numbers_tier(double estimate, string shown, string tier)
	{
		var chip = Chip(new PerceivedRating(estimate, 0));

		chip.TextContent.ShouldBe(shown);
		chip.ClassList.ShouldContain("gfo-tier-chip");
		chip.ClassList.ShouldContain($"gfo-tier-{tier}");
		chip.ClassList.Count(c => c.StartsWith("gfo-tier-", StringComparison.Ordinal)).ShouldBe(2, "The chip and exactly one tier.");
	}

	[Fact]
	public void A_sigma_changes_nothing_until_M6()
	{
		// D-078 #13: σ is 0 until M6, which adds the range after the chip. Until then the chip is the estimate alone.
		var exact = Chip(new PerceivedRating(81.2, 0));
		var fuzzy = Chip(new PerceivedRating(81.2, 3.5));

		fuzzy.OuterHtml.ShouldBe(exact.OuterHtml);
	}

	[Fact]
	public void It_is_one_span_that_passes_its_class_and_attributes_through()
	{
		var cut = Render<RatingChip>(p => p
			.Add(c => c.Value, new PerceivedRating(81, 0))
			.Add(c => c.Class, "ovr")
			.AddUnmatched("data-testid", "chip"));

		cut.Nodes.Length.ShouldBe(1);
		var chip = cut.Find("span");
		chip.ClassList.ShouldContain("ovr");
		chip.GetAttribute("data-testid").ShouldBe("chip");
		chip.Children.Length.ShouldBe(0);
	}

	private AngleSharp.Dom.IElement Chip(PerceivedRating rating) =>
		Render<RatingChip>(p => p.Add(c => c.Value, rating)).Find("span");
}

/// <summary>M1 WP7 slice 1 (guide §4): the potential grade as a pill, colored by its letter (§9 Q3).</summary>
public sealed class PotentialChipTests : BunitContext
{
	[Theory]
	[InlineData(PotentialGrade.APlus, "A+", "elite")]
	[InlineData(PotentialGrade.A, "A", "elite")]
	[InlineData(PotentialGrade.AMinus, "A−", "elite")]
	[InlineData(PotentialGrade.BPlus, "B+", "good")]
	[InlineData(PotentialGrade.B, "B", "good")]
	[InlineData(PotentialGrade.BMinus, "B−", "good")]
	[InlineData(PotentialGrade.CPlus, "C+", "avg")]
	[InlineData(PotentialGrade.C, "C", "avg")]
	[InlineData(PotentialGrade.CMinus, "C−", "avg")]
	[InlineData(PotentialGrade.D, "D", "below")]
	[InlineData(PotentialGrade.F, "F", "poor")]
	public void Each_grade_shows_its_text_in_its_letters_tier(PotentialGrade grade, string text, string tier)
	{
		var chip = Render<PotentialChip>(p => p.Add(c => c.Value, new PotentialRange(grade, grade, grade))).Find("span");

		chip.TextContent.ShouldBe(text);
		chip.ClassList.ShouldContain("gfo-tier-chip");
		chip.ClassList.ShouldContain($"gfo-tier-{tier}");
	}

	[Fact]
	public void The_spread_waits_for_M6()
	{
		// "likely B to A−" arrives with σ > 0 (F-03 §4.6). Until then only the grade shows.
		var chip = Render<PotentialChip>(p => p
			.Add(c => c.Value, new PotentialRange(PotentialGrade.B, PotentialGrade.BMinus, PotentialGrade.AMinus))
			.Add(c => c.Class, "pot")
			.AddUnmatched("data-testid", "grade")).Find("span");

		chip.TextContent.ShouldBe("B");
		chip.ClassList.ShouldContain("gfo-tier-good");
		chip.ClassList.ShouldContain("pot");
		chip.GetAttribute("data-testid").ShouldBe("grade");
	}
}

/// <summary>M1 WP7 slice 1 (guide §4): a position's abbreviation on Surface 3. Position's names are the abbreviations.</summary>
public sealed class PositionPillTests : BunitContext
{
	[Theory]
	[InlineData(Position.QB, "QB")]
	[InlineData(Position.RB, "RB")]
	[InlineData(Position.FB, "FB")]
	[InlineData(Position.WR, "WR")]
	[InlineData(Position.TE, "TE")]
	[InlineData(Position.OT, "OT")]
	[InlineData(Position.OG, "OG")]
	[InlineData(Position.C, "C")]
	[InlineData(Position.EDGE, "EDGE")]
	[InlineData(Position.DT, "DT")]
	[InlineData(Position.LB, "LB")]
	[InlineData(Position.CB, "CB")]
	[InlineData(Position.S, "S")]
	[InlineData(Position.K, "K")]
	[InlineData(Position.P, "P")]
	[InlineData(Position.LS, "LS")]
	public void Every_position_shows_its_abbreviation(Position position, string shown)
	{
		var pill = Render<PositionPill>(p => p.Add(c => c.Value, position)).Find("span");

		pill.TextContent.ShouldBe(shown);
		pill.ClassList.ShouldContain("gfo-position-pill");
	}

	[Fact]
	public void The_rows_above_cover_every_position() =>
		Enum.GetValues<Position>().Length.ShouldBe(16, "A new position needs a row in Every_position_shows_its_abbreviation.");

	[Fact]
	public void It_passes_its_class_and_attributes_through()
	{
		var pill = Render<PositionPill>(p => p
			.Add(c => c.Value, Position.QB)
			.Add(c => c.Class, "pos")
			.AddUnmatched("data-testid", "pill")).Find("span");

		pill.ClassList.ShouldContain("pos");
		pill.GetAttribute("data-testid").ShouldBe("pill");
	}
}
