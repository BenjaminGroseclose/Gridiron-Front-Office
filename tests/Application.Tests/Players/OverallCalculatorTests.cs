using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// OVR, its display and the potential grade (M1 WP4 guide §4.1, F-03 §4.5–4.6): a plain weighted sum per position,
/// rounded half away from zero for screens, graded on the displayed peak.
/// </summary>
public sealed class OverallCalculatorTests
{
	private const double Tolerance = 1e-9;

	private static readonly IReadOnlyList<PositionProfile> Positions = ShippedSeedData.Instance.Positions.Positions;
	private static readonly IReadOnlyList<GradeBand> Bands = ShippedSeedData.Instance.Generation.Hidden.PotentialGrades;

	[Fact]
	public void A_hand_built_quarterback_rates_the_weighted_sum_of_his_ratings()
	{
		var quarterback = Positions.Single(p => p.Position == Position.QB);
		var ratings = Ratings(attribute => 20 + (int)attribute * 7 % 75);

		var expected = Enum.GetValues<PlayerAttribute>().Sum(a => quarterback.OvrWeights.GetValueOrDefault(a) * ratings[a]);

		OverallCalculator.Overall(quarterback, ratings).ShouldBe(expected, Tolerance);
	}

	[Fact]
	public void All_fifty_ratings_rate_fifty_at_every_position()
	{
		var ratings = Ratings(_ => 50);

		Positions
			.Select(p => (p.Position, Overall: OverallCalculator.Overall(p, ratings)))
			.Where(p => Math.Abs(p.Overall - 50) > Tolerance)
			.Select(p => $"{p.Position}: {p.Overall}")
			.ShouldBeEmpty();
	}

	[Fact]
	public void Raising_a_weighted_attribute_raises_ovr_and_an_unweighted_one_does_not()
	{
		var offenders =
			from profile in Positions
			from attribute in Enum.GetValues<PlayerAttribute>()
			let raised = OverallCalculator.Overall(profile, Ratings(a => a == attribute ? 60 : 50))
			let weighted = profile.OvrWeights.ContainsKey(attribute)
			where weighted ? raised <= 50 : Math.Abs(raised - 50) > Tolerance
			select $"{profile.Position} {attribute} ({(weighted ? "weighted" : "unweighted")}): OVR {raised}";

		offenders.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(74.5, 75)]
	[InlineData(74.49, 74)]
	[InlineData(0.5, 1)]
	[InlineData(99.6, 99)]
	[InlineData(120.0, 99)]
	[InlineData(-0.4, 0)]
	public void Display_rounds_half_away_from_zero_within_0_to_99(double rating, int shown) =>
		OverallCalculator.Display(rating).ShouldBe(shown);

	[Theory]
	[InlineData(92.0, PotentialGrade.APlus)]
	[InlineData(91.5, PotentialGrade.APlus)]   // shows as 92
	[InlineData(91.49, PotentialGrade.A)]
	[InlineData(91.0, PotentialGrade.A)]
	[InlineData(57.0, PotentialGrade.F)]
	[InlineData(0.0, PotentialGrade.F)]
	public void Grade_reads_the_displayed_peak(double peak, PotentialGrade grade) =>
		OverallCalculator.Grade(peak, Bands).ShouldBe(grade);

	[Fact]
	public void Every_band_starts_exactly_at_its_minimum()
	{
		var offenders = new List<string>();
		for (var i = 0; i < Bands.Count; i++)
		{
			var atMinimum = OverallCalculator.Grade(Bands[i].MinPeakOvr, Bands);
			if (atMinimum != Bands[i].Grade)
			{
				offenders.Add($"{Bands[i].MinPeakOvr} grades {atMinimum}, not {Bands[i].Grade}");
			}

			if (i + 1 < Bands.Count && OverallCalculator.Grade(Bands[i].MinPeakOvr - 1, Bands) is var below && below != Bands[i + 1].Grade)
			{
				offenders.Add($"{Bands[i].MinPeakOvr - 1} grades {below}, not {Bands[i + 1].Grade}");
			}
		}

		offenders.ShouldBeEmpty();
	}

	private static Dictionary<PlayerAttribute, double> Ratings(Func<PlayerAttribute, double> value) =>
		Enum.GetValues<PlayerAttribute>().ToDictionary(a => a, value);
}
