using System.Text.Json;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// One generated player (M1 WP4 guide §4.2 and §5's slice 2). The statistical checks read one fixed sample, 200 players
/// for every position and tier from seed 42, so they never flake.
/// </summary>
public sealed class PlayerGeneratorTests
{
	private const ulong LeagueSeed = 42;
	private const int Season = 2026;
	private const int PerPositionAndTier = 200;

	private static readonly ISeedData Data = ShippedSeedData.Instance;
	private static readonly GenerationSettings Generation = Data.Generation;
	private static readonly Dictionary<Position, PositionProfile> Profiles = Data.Positions.Positions.ToDictionary(p => p.Position);
	private static readonly PlayerGenerator Generator = new(Data);

	private static readonly Measurable[] Drills =
		[Measurable.Forty, Measurable.TenSplit, Measurable.ThreeCone, Measurable.Shuttle, Measurable.Vertical, Measurable.BroadJump];
	private static readonly Measurable[] Timed = [Measurable.Forty, Measurable.TenSplit, Measurable.ThreeCone, Measurable.Shuttle];

	/// <summary>The ratings the measurables make (Speed, Acceleration, Agility, Strength).</summary>
	private static readonly PlayerAttribute[] Physical = [.. Generation.Athletic.Ratings.Select(m => m.Attribute).Distinct()];

	/// <summary>Every position and tier, 200 each, in slot order, with no team strength.</summary>
	private static readonly IReadOnlyList<GeneratedPlayer> Sample =
	[
		.. (from position in Enum.GetValues<Position>()
			from tier in Enum.GetValues<PlayerTier>()
			from _ in Enumerable.Range(0, PerPositionAndTier)
			select (position, tier))
			.Select((p, slot) => Generator.Generate(LeagueSeed, Season, slot, p.position, p.tier, 0)),
	];

	// ---- Determinism ----

	[Fact]
	public void The_same_inputs_give_the_same_player_and_any_other_input_another()
	{
		var player = Generator.Generate(LeagueSeed, Season, 7, Position.WR, PlayerTier.Starter, 0.3);

		Json(new PlayerGenerator(Data).Generate(LeagueSeed, Season, 7, Position.WR, PlayerTier.Starter, 0.3)).ShouldBe(Json(player));

		// Compare what was drawn: the slot and the season are fields too, so whole players differ even from the same draws.
		Draws(Generator.Generate(LeagueSeed, Season, 8, Position.WR, PlayerTier.Starter, 0.3)).ShouldNotBe(Draws(player));
		Draws(Generator.Generate(LeagueSeed + 1, Season, 7, Position.WR, PlayerTier.Starter, 0.3)).ShouldNotBe(Draws(player));
		Draws(Generator.Generate(LeagueSeed, Season + 1, 7, Position.WR, PlayerTier.Starter, 0.3)).ShouldNotBe(Draws(player));

		static string Draws(GeneratedPlayer player) => JsonSerializer.Serialize(player.Measurables);
	}

	// ---- Ranges ----

	[Fact]
	public void Every_rating_and_trait_is_filled_in_enum_order()
	{
		Sample
			.Where(p => !p.Ratings.Keys.SequenceEqual(Enum.GetValues<PlayerAttribute>())
						|| !p.Personality.Keys.SequenceEqual(Enum.GetValues<PersonalityTrait>())
						|| !p.Measurables.Keys.SequenceEqual(Enum.GetValues<Measurable>()))
			.Select(p => $"slot {p.Slot}: ratings {string.Join(",", p.Ratings.Keys)}")
			.Take(5)
			.ShouldBeEmpty();
	}

	[Fact]
	public void Every_rating_and_trait_is_within_0_to_100()
	{
		var offenders =
			from player in Sample
			from value in player.Ratings.Select(r => (Name: r.Key.ToString(), r.Value))
				.Concat(player.Personality.Select(t => (Name: t.Key.ToString(), t.Value)))
			where value.Value is < 0 or > 100
			select $"slot {player.Slot} {value.Name}: {value.Value}";

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void The_age_fits_the_position_and_the_birthday_gives_it_on_july_1()
	{
		var july1 = new DateOnly(Season, 7, 1);
		var offenders = new List<string>();
		foreach (var player in Sample)
		{
			var age = Profiles[player.Position].Age;
			if (player.Age < age.Min || player.Age > age.Max)
			{
				offenders.Add($"slot {player.Slot} {player.Position}: age {player.Age} outside {age.Min}–{age.Max}");
			}

			if (AgeOn(player.BirthDate, july1) is var shown && shown != player.Age)
			{
				offenders.Add($"slot {player.Slot}: born {player.BirthDate}, {shown} on July 1, drawn {player.Age}");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Experience_counts_from_an_entry_age()
	{
		var entry = Generation.Draft.EntryAge;

		Sample
			.Where(p => p.Experience < 0 || p.Age - p.Experience < entry.Min || p.Age - p.Experience > entry.Max)
			.Select(p => $"slot {p.Slot}: age {p.Age}, experience {p.Experience}")
			.ShouldBeEmpty();
	}

	[Fact]
	public void Potential_is_never_below_ovr()
	{
		Sample
			.Where(p => p.Hidden.Potential < Overall(p))
			.Select(p => $"slot {p.Slot}: potential {p.Hidden.Potential} under OVR {Overall(p)}")
			.ShouldBeEmpty();
	}

	// ---- The combine ----

	[Fact]
	public void A_physical_line_runs_through_its_best_and_worst_points_without_turning_back()
	{
		var offenders = new List<string>();
		foreach (var map in Generation.Athletic.Ratings)
		{
			foreach (var point in new[] { map.Best, map.Worst })
			{
				var rating = PlayerGenerator.PhysicalLine(map, point.Value);
				if (Math.Abs(rating - point.Rating) > 1e-9)
				{
					offenders.Add($"{map.Measurable} at {point.Value}: {rating}, not {point.Rating}");
				}
			}

			var line = Enumerable.Range(0, 101)
				.Select(i => PlayerGenerator.PhysicalLine(map, map.Worst.Value + ((map.Best.Value - map.Worst.Value) * i / 100)))
				.ToList();
			if (line.Zip(line.Skip(1)).Any(step => step.Second < step.First))
			{
				offenders.Add($"{map.Measurable}: the line turns back between its worst and best points");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Physical_ratings_sit_within_the_noise_of_their_measurable_lines()
	{
		// F-03 §4.3's near-exact ±3. Only lines well inside 0–100, so the clamp can't bend the noise.
		var noise = Generation.Athletic.PhysicalNoiseSd;
		var offenders = new List<string>();
		foreach (var attribute in Physical)
		{
			var maps = Generation.Athletic.Ratings.Where(m => m.Attribute == attribute).ToList();
			var residuals = Sample
				.Select(p => (Rating: p.Ratings[attribute], Line: maps.Average(m => PlayerGenerator.PhysicalLine(m, p.Measurables[m.Measurable]))))
				.Where(r => r.Line is > 10 and < 90)
				.Select(r => r.Rating - r.Line)
				.ToList();

			var (mean, sd) = MeanAndSd(residuals);
			if (residuals.Count < 1_000 || Math.Abs(mean) > 0.3 || Math.Abs(sd - noise) > 0.3)
			{
				offenders.Add($"{attribute}: {residuals.Count} players, mean {mean:0.00}, sd {sd:0.00} against noise {noise}");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Any_two_drills_correlate_at_the_drill_correlation()
	{
		var rho = Generation.Athletic.DrillCorrelation;
		var z = Drills.ToDictionary(d => d, d => Sample.Select(p => Z(p, d)).ToArray());

		var offenders =
			from i in Enumerable.Range(0, Drills.Length)
			from j in Enumerable.Range(i + 1, Drills.Length - i - 1)
			let r = Correlation(z[Drills[i]], z[Drills[j]])
			where Math.Abs(r - rho) > 0.05
			select $"{Drills[i]} and {Drills[j]}: {r:0.000}, not {rho}";

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void A_starter_runs_a_faster_forty_than_a_fringe_player_at_his_position()
	{
		Enum.GetValues<Position>()
			.Select(position => (Position: position, Starter: MeanForty(position, PlayerTier.Starter), Fringe: MeanForty(position, PlayerTier.Fringe)))
			.Where(p => p.Starter >= p.Fringe)
			.Select(p => $"{p.Position}: Starters {p.Starter:0.000}, Fringe {p.Fringe:0.000}")
			.ShouldBeEmpty();

		static double MeanForty(Position position, PlayerTier tier) =>
			Sample.Where(p => p.Position == position && p.Tier == tier).Average(p => p.Measurables[Measurable.Forty]);
	}

	// ---- Tiers and colleges ----

	[Fact]
	public void College_tiers_follow_their_shares_for_each_player_tier()
	{
		var offenders =
			from tier in Enum.GetValues<PlayerTier>()
			let players = Sample.Where(p => p.Tier == tier).ToList()
			from share in Generation.CollegeTiers[tier]
			let actual = players.Count(p => p.College.Tier == share.Key) / (double)players.Count
			where Math.Abs(actual - share.Value) > 0.03
			select $"{tier} players from {share.Key} colleges: {actual:P1}, share {share.Value:P0}";

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Players_22_or_younger_have_about_8_of_growth_ahead()
	{
		var growth = Sample.Where(p => p.Age <= 22).Select(p => p.Hidden.Potential - Overall(p)).ToList();

		growth.Count.ShouldBeGreaterThan(300);
		growth.Average().ShouldBe(8, tolerance: 0.5);
	}

	// ---- The label ----

	[Fact]
	public void The_recomputed_label_matches_the_drawn_archetype_for_nine_players_in_ten()
	{
		// 97.5% in the reference run (WP4 guide §4.1).
		var players = Sample.Where(p => Profiles[p.Position].Archetypes.Count > 1).ToList();
		var matches = players.Count(p => OverallCalculator.Archetype(Profiles[p.Position], Generation.Skills, p.Ratings) == p.Archetype);

		(matches / (double)players.Count).ShouldBeGreaterThanOrEqualTo(0.9);
	}

	// ---- Team strength (D-082) ----

	[Fact]
	public void Team_strength_raises_ovr_through_the_skills_and_changes_nothing_else()
	{
		var offenders = new List<string>();
		for (var slot = 0; slot < 200; slot++)
		{
			var level = Generator.Generate(LeagueSeed, Season, slot, Position.QB, PlayerTier.Starter, 0);
			var strong = Generator.Generate(LeagueSeed, Season, slot, Position.QB, PlayerTier.Starter, 0.5);

			if (Overall(strong) <= Overall(level))
			{
				offenders.Add($"slot {slot}: OVR {Overall(level):0.00} at 0, {Overall(strong):0.00} at +0.5");
			}

			// Every draw is the same: only the skills, and the potential built on them, may differ.
			var sameDraws = level with { Ratings = strong.Ratings, Hidden = level.Hidden with { Potential = strong.Hidden.Potential } };
			if (Json(sameDraws) != Json(strong))
			{
				offenders.Add($"slot {slot}: more than the skills changed");
			}

			var archetype = Profiles[Position.QB].Archetypes.Single(a => a.Name == level.Archetype);
			offenders.AddRange(Enum.GetValues<PlayerAttribute>()
				.Where(a => !OnPosition(archetype, a) && level.Ratings[a] != strong.Ratings[a])
				.Select(a => $"slot {slot}: {a} moved, but team strength only moves on-position skills"));
		}

		offenders.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(Position.QB)]   // spread scale 1
	[InlineData(Position.K)]    // 0.72 (D-088)
	public void The_lift_is_the_strength_times_the_young_or_the_veteran_weight(Position position)
	{
		// An unclamped player's OVR moves by strength × weight × the tier's spread × his position's spread scale × the OVR
		// weight of his on-position skills. Backups, so few skills sit at the clamp.
		const double strength = 0.5;
		var profile = Profiles[position];
		var spread = Generation.Tiers[PlayerTier.Backup].Spread;
		var rookieYears = Data.Contracts.RookieScale.Years;

		var offenders = new List<string>();
		var (young, veterans) = (0, 0);
		for (var slot = 0; slot < 400; slot++)
		{
			var level = Generator.Generate(LeagueSeed, Season, slot, position, PlayerTier.Backup, 0);
			var strong = Generator.Generate(LeagueSeed, Season, slot, position, PlayerTier.Backup, strength);
			var archetype = profile.Archetypes.Single(a => a.Name == level.Archetype);
			var skills = Enum.GetValues<PlayerAttribute>().Where(a => OnPosition(archetype, a)).ToList();

			if (skills.Any(a => level.Ratings[a] is <= 0 or >= 100 || strong.Ratings[a] is <= 0 or >= 100))
			{
				continue;
			}

			var isYoung = level.Experience < rookieYears;
			var weight = isYoung ? Generation.TeamStrength.YoungWeight : Generation.TeamStrength.VeteranWeight;
			var expected = strength * weight * spread * profile.SpreadScale * skills.Sum(a => profile.OvrWeights.GetValueOrDefault(a));
			var lift = Overall(strong) - Overall(level);
			if (Math.Abs(lift - expected) > 1e-9)
			{
				offenders.Add($"slot {slot} ({(isYoung ? "young" : "veteran")}): lift {lift:0.0000}, expected {expected:0.0000}");
			}

			if (isYoung)
			{
				young++;
			}
			else
			{
				veterans++;
			}
		}

		young.ShouldBeGreaterThan(20);
		veterans.ShouldBeGreaterThan(20);
		offenders.ShouldBeEmpty();
	}

	// ---- Helpers ----

	private static double Overall(GeneratedPlayer player) => OverallCalculator.Overall(Profiles[player.Position], player.Ratings);

	private static string Json(GeneratedPlayer player) => JsonSerializer.Serialize(player);

	/// <summary>A skill the quality draw shapes: listed by the archetype or common to every position, and not physical.</summary>
	private static bool OnPosition(Archetype archetype, PlayerAttribute attribute) =>
		!Physical.Contains(attribute) && (archetype.Skills.ContainsKey(attribute) || Generation.Skills.Common.ContainsKey(attribute));

	/// <summary>The drill's normal draw taken back out of its value: sign · (value − mean) / sd, minus the tier's shift.</summary>
	private static double Z(GeneratedPlayer player, Measurable drill)
	{
		var distribution = Profiles[player.Position].Measurables[drill];
		var sign = Timed.Contains(drill) ? -1 : 1;
		return (sign * (player.Measurables[drill] - distribution.Mean) / distribution.Sd) - Generation.Tiers[player.Tier].AthleticShift;
	}

	private static int AgeOn(DateOnly birthDate, DateOnly date)
	{
		var age = date.Year - birthDate.Year;
		return birthDate.AddYears(age) > date ? age - 1 : age;
	}

	private static (double Mean, double Sd) MeanAndSd(IReadOnlyList<double> values)
	{
		var mean = values.Average();
		return (mean, Math.Sqrt(values.Average(v => (v - mean) * (v - mean))));
	}

	private static double Correlation(double[] x, double[] y)
	{
		var (meanX, sdX) = MeanAndSd(x);
		var (meanY, sdY) = MeanAndSd(y);
		return x.Zip(y).Average(p => (p.First - meanX) * (p.Second - meanY)) / (sdX * sdY);
	}
}
