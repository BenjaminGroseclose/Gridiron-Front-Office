using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Tests.Leagues;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// M1 WP6 slice 1 (guide §5): the fog-of-war boundary as M1 builds it. Every sigma is 0 and every estimate the true
/// value, personality shows only for the viewer's own players, and a roster's strength is its starters' mean OVR on
/// each side (D-092). Seed 1's league, on the shipped data.
/// </summary>
public sealed class PerceptionTests
{
	private static readonly GeneratedLeague League = GeneratedLeagues.Twenty[0].League;
	private static readonly Perception Perception = new(ShippedSeedData.Instance);
	private static readonly Dictionary<Position, PositionProfile> Profiles =
		ShippedSeedData.Instance.Positions.Positions.ToDictionary(p => p.Position);

	private static readonly TeamID Own = new(1);
	private static readonly TeamID Rival = new(2);

	/// <summary>Team 1 (whose players include the viewer's own), team 2, and no one: an outsider before the pick.</summary>
	public static TheoryData<int?> Viewers { get; } = new(new int?[] { 1, 2, null });

	[Theory]
	[MemberData(nameof(Viewers))]
	public void Every_sigma_is_zero_and_every_estimate_the_true_rating_whoever_looks(int? viewerID)
	{
		var viewer = viewerID is { } id ? new TeamID(id) : (TeamID?)null;
		var problems = new List<string>();

		foreach (var player in Everyone())
		{
			var seen = Perception.Perceive(viewer, player);

			seen.PlayerID.ShouldBe(player.PlayerID);
			seen.Attributes.Keys.ShouldBe(Enum.GetValues<PlayerAttribute>(), ignoreOrder: true);
			foreach (var (attribute, rating) in seen.Attributes)
			{
				if (rating != new PerceivedRating(player.Attributes[attribute], 0))
				{
					problems.Add($"Player {player.PlayerID.Value}, {attribute}: {rating}, not ({player.Attributes[attribute]}, 0)");
				}
			}

			if (seen.Overall.Sigma != 0 || seen.Potential != new PerceivedRating(player.Potential, 0))
			{
				problems.Add($"Player {player.PlayerID.Value}: OVR {seen.Overall}, potential {seen.Potential}");
			}
		}

		problems.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void Overall_is_the_calculators_over_the_true_ratings_for_the_players_position()
	{
		var wrong = Everyone()
			.Select(p => (p, Seen: Perception.Perceive(null, p).Overall.Estimate, Expected: OverallCalculator.Overall(Profiles[p.Position], p.Attributes)))
			.Where(x => x.Seen != x.Expected)
			.Select(x => $"Player {x.p.PlayerID.Value} ({x.p.Position}): {x.Seen}, not {x.Expected}");

		wrong.ShouldBeEmpty();
	}

	[Fact]
	public void Personality_shows_only_for_the_viewers_own_players()
	{
		var own = Snapshot(Own);
		var rival = Snapshot(Rival);
		var freeAgent = Snapshot(null);

		Perception.Perceive(Own, own).Personality.ShouldNotBeNull().Keys.ShouldBe(Enum.GetValues<PersonalityTrait>(), ignoreOrder: true);
		Perception.Perceive(Own, rival).Personality.ShouldBeNull("A rival's personality is Unknown (D-092 #2).");
		Perception.Perceive(Own, freeAgent).Personality.ShouldBeNull("A free agent is no viewer's own player.");
		Perception.Perceive(null, own).Personality.ShouldBeNull("Before the pick the user is an outsider (D-092 #1).");
		Perception.Perceive(null, freeAgent).Personality.ShouldBeNull("Null isn't a team: an outsider doesn't own the free agents.");
	}

	[Fact]
	public void A_trait_is_low_below_35_high_above_65_and_average_between()
	{
		var bands = ShippedSeedData.Instance.Generation.Personality;
		(bands.LowBelow, bands.HighAbove).ShouldBe((35, 65), "The cases below are written for generation.json's bands.");

		var traits = new Dictionary<PersonalityTrait, double>
		{
			[PersonalityTrait.WorkEthic] = 34.9,
			[PersonalityTrait.Coachability] = 35,
			[PersonalityTrait.Greed] = 65,
			[PersonalityTrait.Loyalty] = 65.1,
			[PersonalityTrait.Competitiveness] = 0,
			[PersonalityTrait.Ego] = 100,
		};
		var player = Snapshot(Own) with { Personality = traits };

		Perception.Perceive(Own, player).Personality.ShouldBe(new Dictionary<PersonalityTrait, TraitLevel>
		{
			[PersonalityTrait.WorkEthic] = TraitLevel.Low,
			[PersonalityTrait.Coachability] = TraitLevel.Average,
			[PersonalityTrait.Greed] = TraitLevel.Average,
			[PersonalityTrait.Loyalty] = TraitLevel.High,
			[PersonalityTrait.Competitiveness] = TraitLevel.Low,
			[PersonalityTrait.Ego] = TraitLevel.High,
		}, ignoreOrder: true);
	}

	[Fact]
	public void A_rosters_strength_is_each_positions_best_up_to_its_starters_averaged_on_each_side()
	{
		var problems = new List<string>();

		foreach (var (team, index) in League.Teams.Select((t, i) => (t, i)))
		{
			var roster = Roster(team, new TeamID(index + 1));

			// Written out from positions.json's counts: 11 on offense (no FB), 11 on defense, and K, P and LS only overall
			var starters = roster
				.GroupBy(p => p.Position)
				.SelectMany(g => g.Select(Ovr).OrderByDescending(o => o).Take(Profiles[g.Key].Roster.Starters).Select(o => (g.Key, Ovr: o)))
				.ToList();
			var offense = starters.Where(s => s.Key.Side() == SideOfBall.Offense).ToList();
			var defense = starters.Where(s => s.Key.Side() == SideOfBall.Defense).ToList();
			(offense.Count, defense.Count, starters.Count).ShouldBe((11, 11, 25), $"{team.Definition.Abbr}'s roster fills every starting spot.");

			var expected = new PerceivedRosterRating(
				new PerceivedRating(offense.Average(s => s.Ovr), 0),
				new PerceivedRating(defense.Average(s => s.Ovr), 0),
				new PerceivedRating(starters.Average(s => s.Ovr), 0));
			var actual = Perception.PerceiveRoster(null, roster);
			if (!Close(actual, expected))
			{
				problems.Add($"{team.Definition.Abbr}: {actual}, not {expected}");
			}
		}

		problems.ShouldBeEmpty();
	}

	[Fact]
	public void A_fullback_never_starts_and_a_backup_never_counts()
	{
		var roster = Roster(League.Teams[0], Own);
		var before = Perception.PerceiveRoster(Own, roster);

		// A fullback better than anyone on the roster, and a receiver worse than the three who start
		var fullback = roster[0] with { PlayerID = new PlayerID(9001), Position = Position.FB, Attributes = Uniform(99) };
		var backup = roster[0] with { PlayerID = new PlayerID(9002), Position = Position.WR, Attributes = Uniform(1) };

		Perception.PerceiveRoster(Own, [.. roster, fullback, backup]).ShouldBe(before);
	}

	[Fact]
	public void A_better_starter_raises_his_side_and_the_overall_but_not_the_other_side()
	{
		var roster = Roster(League.Teams[0], Own);
		var before = Perception.PerceiveRoster(Own, roster);
		var star = roster[0] with { PlayerID = new PlayerID(9003), Position = Position.CB, Attributes = Uniform(99) };

		var after = Perception.PerceiveRoster(Own, [.. roster, star]);

		after.Offense.ShouldBe(before.Offense);
		after.Defense.Estimate.ShouldBeGreaterThan(before.Defense.Estimate);
		after.Overall.Estimate.ShouldBeGreaterThan(before.Overall.Estimate);
	}

	[Fact]
	public void Kickers_punters_and_long_snappers_count_only_overall()
	{
		var roster = Roster(League.Teams[0], Own);
		var specialists = new[] { Position.K, Position.P, Position.LS };
		var with = Perception.PerceiveRoster(Own, roster);

		var without = Perception.PerceiveRoster(Own, [.. roster.Where(p => !specialists.Contains(p.Position))]);

		(without.Offense, without.Defense).ShouldBe((with.Offense, with.Defense));
		var specialistOvrs = roster.Where(p => specialists.Contains(p.Position)).Select(Ovr).ToList();
		specialistOvrs.Count.ShouldBe(3);
		(with.Overall.Estimate * 25).ShouldBe((without.Overall.Estimate * 22) + specialistOvrs.Sum(), 1e-9);
	}

	[Fact]
	public void An_empty_roster_is_zero_on_every_side() =>
		Perception.PerceiveRoster(Own, []).ShouldBe(new PerceivedRosterRating(new(0, 0), new(0, 0), new(0, 0)));

	/// <summary>Every generated player of seed 1 as a snapshot: team i + 1's roster, then the free agents with no team.</summary>
	private static IEnumerable<PlayerSnapshot> Everyone() =>
		League.Teams.SelectMany((t, i) => Roster(t, new TeamID(i + 1)))
			.Concat(League.FreeAgents.Select(p => p.ToPlayer(new PlayerID(p.Slot + 1), null, PlayerStatus.FreeAgent, new CollegeID(1), null).ToSnapshot()));

	private static List<PlayerSnapshot> Roster(GeneratedTeam team, TeamID id) =>
		[.. team.Players.Select(p => p.ToPlayer(new PlayerID(p.Slot + 1), id, PlayerStatus.Active, new CollegeID(1), null).ToSnapshot())];

	/// <summary>Team 1's first player, on <paramref name="team"/> (null: a free agent).</summary>
	private static PlayerSnapshot Snapshot(TeamID? team) => Roster(League.Teams[0], Own)[0] with { TeamID = team };

	private static double Ovr(PlayerSnapshot p) => OverallCalculator.Overall(Profiles[p.Position], p.Attributes);

	private static Dictionary<PlayerAttribute, double> Uniform(double value) =>
		Enum.GetValues<PlayerAttribute>().ToDictionary(a => a, _ => value);

	private static bool Close(PerceivedRosterRating a, PerceivedRosterRating b) =>
		Close(a.Offense, b.Offense) && Close(a.Defense, b.Defense) && Close(a.Overall, b.Overall);

	// The mean of the same numbers summed in another order can differ in the last bit
	private static bool Close(PerceivedRating a, PerceivedRating b) =>
		Math.Abs(a.Estimate - b.Estimate) < 1e-9 && a.Sigma == b.Sigma;
}
