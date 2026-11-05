using GridironFrontOffice.Core.Players;
using GridironFrontOffice.UI.Football;

namespace GridironFrontOffice.UI.Tests.Football;

/// <summary>
/// M1 WP7 slice 1 (guide §4): the five rating tiers (VISUAL_DESIGN.md §3.4), the potential grades on the same scale by
/// their letter (§9 Q3), the grades' text, and the class names gfo-tokens.css defines for each tier.
/// </summary>
public sealed class RatingTiersTests
{
	[Theory]
	[InlineData(0, RatingTier.Poor)]
	[InlineData(59, RatingTier.Poor)]
	[InlineData(60, RatingTier.Below)]
	[InlineData(69, RatingTier.Below)]
	[InlineData(70, RatingTier.Average)]
	[InlineData(79, RatingTier.Average)]
	[InlineData(80, RatingTier.Good)]
	[InlineData(89, RatingTier.Good)]
	[InlineData(90, RatingTier.Elite)]
	[InlineData(99, RatingTier.Elite)]
	public void A_shown_rating_takes_its_tier_on_both_sides_of_each_edge(int shown, RatingTier tier) =>
		RatingTiers.Of(shown).ShouldBe(tier);

	[Theory]
	[InlineData(PotentialGrade.APlus, "A+", RatingTier.Elite)]
	[InlineData(PotentialGrade.A, "A", RatingTier.Elite)]
	[InlineData(PotentialGrade.AMinus, "A−", RatingTier.Elite)]
	[InlineData(PotentialGrade.BPlus, "B+", RatingTier.Good)]
	[InlineData(PotentialGrade.B, "B", RatingTier.Good)]
	[InlineData(PotentialGrade.BMinus, "B−", RatingTier.Good)]
	[InlineData(PotentialGrade.CPlus, "C+", RatingTier.Average)]
	[InlineData(PotentialGrade.C, "C", RatingTier.Average)]
	[InlineData(PotentialGrade.CMinus, "C−", RatingTier.Average)]
	[InlineData(PotentialGrade.D, "D", RatingTier.Below)]
	[InlineData(PotentialGrade.F, "F", RatingTier.Poor)]
	public void Each_grade_has_its_text_and_its_letters_tier(PotentialGrade grade, string text, RatingTier tier)
	{
		grade.Text().ShouldBe(text);
		RatingTiers.OfGrade(grade).ShouldBe(tier);
	}

	[Fact]
	public void Every_grade_has_a_text_of_its_own_with_a_real_minus()
	{
		var texts = Enum.GetValues<PotentialGrade>().Select(g => g.Text()).ToList();

		texts.Count.ShouldBe(11, "A new grade needs a row above, and a line in PotentialGrades.Text.");
		texts.Distinct(StringComparer.Ordinal).Count().ShouldBe(texts.Count);
		texts.ShouldAllBe(t => !t.Contains('-'));
	}

	[Theory]
	[InlineData(RatingTier.Elite, "elite")]
	[InlineData(RatingTier.Good, "good")]
	[InlineData(RatingTier.Average, "avg")]
	[InlineData(RatingTier.Below, "below")]
	[InlineData(RatingTier.Poor, "poor")]
	public void Each_tier_names_its_class(RatingTier tier, string token) =>
		tier.Token().ShouldBe(token);

	[Fact]
	public void Gfo_tokens_defines_every_tiers_class_and_the_chip()
	{
		var css = File.ReadAllText(Path.Combine(RepoRoot().FullName, "src", "UI.Components", "wwwroot", "gfo-tokens.css"));

		// One source for tier → color: a token the C# names but the stylesheet lacks paints the chip in no color at all
		foreach (var tier in Enum.GetValues<RatingTier>())
		{
			css.ShouldContain($".gfo-tier-{tier.Token()} {{", Case.Sensitive, $"gfo-tokens.css has no .gfo-tier-{tier.Token()}.");
			css.ShouldContain($"--gfo-rating-{tier.Token()}:", Case.Sensitive, $"gfo-tokens.css has no --gfo-rating-{tier.Token()} token.");
		}

		css.ShouldContain(".gfo-tier-chip {", Case.Sensitive);
		css.ShouldContain("--gfo-quiet-fill:", Case.Sensitive);
	}

	private static DirectoryInfo RepoRoot()
	{
		for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			if (File.Exists(Path.Combine(dir.FullName, "GridironFrontOffice.slnx")))
			{
				return dir;
			}
		}

		throw new InvalidOperationException($"GridironFrontOffice.slnx not found above {AppContext.BaseDirectory}.");
	}
}
