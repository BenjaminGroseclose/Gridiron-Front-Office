using System.Text.Json;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Application.Tests.Leagues;

/// <summary>
/// The league <see cref="LeagueGenerator"/> builds (M1 WP4 guide §5, slice 3's rows), on the shared seeds 1–20: its
/// shape, the roster mix, the strength ladder, the draft classes, jerseys and names, determinism and the distributions.
/// Generating the 20 at all is the jersey row's last check: step 8 throws if a team runs out of numbers.
/// </summary>
public sealed class LeagueShapeTests
{
	private const int Season = GeneratedLeagues.Season;

	private static readonly ISeedData Data = ShippedSeedData.Instance;
	private static readonly GenerationSettings Generation = Data.Generation;
	private static readonly IReadOnlyList<TeamDefinition> Teams = Data.TeamSet.Teams;
	private static readonly int Active = Generation.Roster.Active;
	private static readonly int Pool = Generation.Roster.FreeAgents;
	private static readonly Position[] Positions = Enum.GetValues<Position>();
	private static readonly Dictionary<Position, PositionProfile> Profiles = Data.Positions.Positions.ToDictionary(p => p.Position);

	/// <summary>The positions whose OVR is mostly two kicking skills (§5's exemption).</summary>
	private static readonly HashSet<Position> Kicking = [Position.K, Position.P, Position.LS];

	private static IReadOnlyList<LoggedLeague> Twenty => GeneratedLeagues.Twenty;

	// ---- Shape ----

	[Fact]
	public void Every_league_has_the_team_set_s_teams_of_53_the_free_agent_pool_every_slot_once_and_one_1300()
	{
		var players = Teams.Count * Active + Pool;
		var offenders = new List<string>();

		foreach (var (seed, league, events) in Twenty)
		{
			if (league.Seed != seed || league.Season != Season || !league.Teams.Select(t => t.Definition).SequenceEqual(Teams))
			{
				offenders.Add($"seed {seed}: not seed {seed}'s {Season} league with the team set's teams in its order");
			}

			offenders.AddRange(league.Teams.Where(t => t.Players.Count != Active).Select(t => $"seed {seed} {t.Definition.Abbr}: {t.Players.Count} players"));

			if (league.FreeAgents.Count != Pool)
			{
				offenders.Add($"seed {seed}: {league.FreeAgents.Count} free agents");
			}

			// Step 3's slots: 53 a team, then the free agents, in their list's order
			var slots = Everyone(league).Select(p => p.Slot).Order();
			if (!slots.SequenceEqual(Enumerable.Range(0, players)) || !league.FreeAgents.Select(f => f.Slot).SequenceEqual(Enumerable.Range(Teams.Count * Active, Pool)))
			{
				offenders.Add($"seed {seed}: the slots aren't 0–{players - 1} once each with the free agents' last");
			}

			var generated = events.Where(e => e.EventID == 1300).ToList();
			if (generated.Count != 1 || (int)generated[0].Values["Season"]! != Season || (ulong)generated[0].Values["Seed"]! != seed
				|| (int)generated[0].Values["PlayerCount"]! != players)
			{
				offenders.Add($"seed {seed}: {generated.Count} × 1300, or not with this league's season, seed and {players} players");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void The_free_agents_are_Fringe_and_split_by_largest_remainder()
	{
		// A position's quota is pool × average / 53. Largest remainder: each count is its quota rounded down or up, and no
		// position rounded down has a larger remainder than one rounded up, or the same one and an earlier place in enum order
		var shares = Positions.ToDictionary(p => p, p => Pool * (int)Profiles[p].Roster.Average);
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			var counts = Positions.ToDictionary(p => p, p => league.FreeAgents.Count(f => f.Position == p));
			var down = Positions.Where(p => counts[p] == shares[p] / Active).ToList();
			var up = Positions.Where(p => counts[p] == shares[p] / Active + 1).ToList();

			offenders.AddRange(Positions.Except(down).Except(up).Select(p => $"seed {seed} {p}: {counts[p]} for a quota of {shares[p] / (double)Active:F2}"));
			offenders.AddRange(
				from u in up
				from d in down
				let upRemainder = shares[u] % Active
				let downRemainder = shares[d] % Active
				where downRemainder > upRemainder || (downRemainder == upRemainder && d < u)
				select $"seed {seed}: {u} rounded up (remainder {upRemainder}/{Active}) ahead of {d} ({downRemainder}/{Active})");
			offenders.AddRange(league.FreeAgents.Where(f => f.Tier != PlayerTier.Fringe).Select(f => $"seed {seed}: free agent {f.Slot} is a {f.Tier}"));
		}

		offenders.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void Every_team_is_inside_each_position_s_range_and_floor_and_fields_its_Starters()
	{
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			foreach (var team in league.Teams)
			{
				foreach (var position in Positions)
				{
					var target = Profiles[position].Roster;
					var players = team.Players.Where(p => p.Position == position).ToList();
					var starters = players.Count(p => p.Tier == PlayerTier.Starter);

					if (players.Count < target.Min || players.Count > target.Max || players.Count < target.Floor || starters != target.Starters
						|| players.Any(p => p.Tier == PlayerTier.Fringe))
					{
						offenders.Add($"seed {seed} {team.Definition.Abbr} {position}: {players.Count} ({target.Min}–{target.Max}, floor {target.Floor}), {starters} Starters of {target.Starters}, tiers {string.Join(",", players.Select(p => p.Tier))}");
					}
				}
			}
		}

		offenders.Take(20).ShouldBeEmpty();
	}

	// ---- The roster mix ----

	[Fact]
	public void The_roster_mix_mirrors_each_pair_so_the_league_average_is_the_target_exactly()
	{
		var half = Teams.Count / 2;
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			foreach (var position in Positions)
			{
				var average = Profiles[position].Roster.Average;
				var counts = league.Teams.Select(t => t.Players.Count(p => p.Position == position)).ToList();

				if (counts.Sum() != average * counts.Count)
				{
					offenders.Add($"seed {seed} {position}: {counts.Sum()} in the league, not {average} a team");
				}

				offenders.AddRange(Enumerable.Range(0, half)
					.Where(k => counts[k] + counts[k + half] != 2 * average)
					.Select(k => $"seed {seed} {position}: teams {k} and {k + half} have {counts[k]} + {counts[k + half]}, not 2 × {average}"));
			}
		}

		offenders.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void The_roster_mix_moves_players_in_every_league()
	{
		// A mix that never swaps passes the test above. Every team was off the averages in every seed on 2026-10-29; the
		// test asks for half of them
		var offenders =
			from g in Twenty
			let moved = g.League.Teams.Count(t => Positions.Any(position => t.Players.Count(p => p.Position == position) != Profiles[position].Roster.Average))
			where moved < g.League.Teams.Count / 2
			select $"seed {g.Seed}: {moved} teams off the averages";

		offenders.ShouldBeEmpty();
	}

	// ---- The ladder ----

	[Fact]
	public void The_ladder_is_evenly_spaced_with_mean_0_and_each_seed_shuffles_it()
	{
		var range = Generation.TeamStrength.Range;
		var ladder = Enumerable.Range(0, Teams.Count).Select(i => range * (2 * (i + 0.5) / Teams.Count - 1)).ToList();

		foreach (var (seed, league, _) in Twenty)
		{
			league.Teams.Select(t => t.Strength).Order().ShouldBe(ladder, 1e-12, $"seed {seed}");
			league.Teams.Average(t => t.Strength).ShouldBe(0, 1e-12, $"seed {seed}");
		}

		// Each league's order as places on the ladder: the 20 are all different
		var orders = Twenty.Select(g => string.Join(",", g.League.Teams.Select(t => ladder.FindIndex(x => Math.Abs(x - t.Strength) < 1e-12))));
		orders.Distinct().Count().ShouldBe(Twenty.Count);
	}

	[Fact]
	public void The_ladder_s_top_quarter_fields_better_Starters_than_its_bottom_quarter_after_the_trades()
	{
		// D-082's point: the cap trades don't erase the ladder. The gap averaged 3.79 OVR on 2026-10-29 (2.8–4.8 by seed),
		// and 4.2 in the reference run (§9)
		var gaps = Twenty.Select(g =>
		{
			var ordered = g.League.Teams.OrderBy(t => t.Strength).ToList();
			var quarter = ordered.Count / 4;
			return StartersOvr(ordered.TakeLast(quarter)) - StartersOvr(ordered.Take(quarter));
		}).ToList();

		gaps.Average().ShouldBeGreaterThanOrEqualTo(3.0, $"by seed: {string.Join(", ", gaps.Select(g => $"{g:F1}"))}");

		static double StartersOvr(IEnumerable<GeneratedTeam> teams) => teams.SelectMany(t => t.Players).Where(p => p.Tier == PlayerTier.Starter).Average(Ovr);
	}

	// ---- Draft classes ----

	[Fact]
	public void Each_entry_class_drafts_its_share_at_evenly_spread_picks()
	{
		var picks = Data.Contracts.RookieScale.Rounds.Count * Teams.Count;
		var share = Generation.Draft.DraftedShare;
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			foreach (var draftClass in Everyone(league).GroupBy(p => Season - p.Experience))
			{
				var label = $"seed {seed}, the class of {draftClass.Key}";
				var drafted = draftClass.Where(p => p.Draft is not null).ToList();
				var n = Math.Min(picks, (int)Math.Round(share * draftClass.Count(), MidpointRounding.AwayFromZero));

				// The k-th of n takes pick k × picks / n + 1, so the picks are unique and spread over every round
				if (!drafted.Select(p => p.Draft!.Pick).Order().SequenceEqual(Enumerable.Range(0, n).Select(k => k * picks / n + 1)))
				{
					offenders.Add($"{label}: {drafted.Count} of {draftClass.Count()} drafted, not {n}, or at other picks");
				}

				offenders.AddRange(drafted
					.Where(p => p.Draft!.Season != draftClass.Key || p.Draft.Round != (p.Draft.Pick - 1) / Teams.Count + 1 || !Teams.Contains(p.Draft.Team))
					.Select(p => $"{label}: slot {p.Slot} was drafted in {p.Draft!.Season}, round {p.Draft.Round}, pick {p.Draft.Pick}, by {p.Draft.Team.Abbr}"));
			}
		}

		offenders.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void Better_players_go_earlier_in_the_draft()
	{
		// Over the classes of 30 or more, as a mean because small classes go as low as −0.5. −0.724 over 245 classes on
		// 2026-10-29 (−0.87 to −0.50 by class), and −0.73 in the reference run
		var correlations = (
			from g in Twenty
			from draftClass in Everyone(g.League).GroupBy(p => Season - p.Experience)
			where draftClass.Count() >= 30
			select GeneratedLeagues.Spearman([.. draftClass.Where(p => p.Draft is not null).Select(p => (Ovr(p), (double)p.Draft!.Pick))]))
			.ToList();

		correlations.ShouldNotBeEmpty();
		correlations.Average().ShouldBeLessThan(-0.6);
	}

	[Fact]
	public void A_player_on_his_rookie_deal_was_drafted_by_his_own_team_and_anyone_else_by_a_drawn_one()
	{
		var offenders = new List<string>();
		var (rookies, others, ownTeam) = (0, 0, 0);

		foreach (var (seed, league, _) in Twenty)
		{
			foreach (var team in league.Teams)
			{
				foreach (var player in team.Players.Where(p => p.Draft is not null))
				{
					if (player.Contract!.Type == ContractType.Rookie)
					{
						rookies++;
						if (player.Draft!.Team != team.Definition)
						{
							offenders.Add($"seed {seed} {team.Definition.Abbr}: slot {player.Slot}'s rookie deal is from {player.Draft.Team.Abbr}");
						}
					}
					else
					{
						// Drawn from the 32, so about 1 in 32 is the team he was generated for
						others++;
						ownTeam += player.Draft!.Team == Teams[player.Slot / Active] ? 1 : 0;
					}
				}
			}
		}

		offenders.Take(20).ShouldBeEmpty();
		rookies.ShouldBeGreaterThan(0);
		(ownTeam / (double)others).ShouldBeLessThan(0.1, $"{ownTeam} of {others} veterans were drafted by the team they were generated for");
	}

	// ---- Jerseys and names ----

	[Fact]
	public void Every_rostered_player_wears_a_drawn_number_his_rule_allows_unique_on_his_team_and_no_free_agent_has_one()
	{
		var rules = Data.JerseyRules.Rules.ToDictionary(r => r.Position, r => r.Numbers);
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			foreach (var team in league.Teams)
			{
				offenders.AddRange(team.Players
					.Where(p => p.JerseyNumber is not int n || !rules[p.Position].Any(r => n >= r.From && n <= r.To))
					.Select(p => $"seed {seed} {team.Definition.Abbr}: the {p.Position} in slot {p.Slot} wears {(p.JerseyNumber is null ? "nothing" : $"{p.JerseyNumber}")}"));

				var numbers = team.Players.Select(p => p.JerseyNumber).ToList();
				if (numbers.Distinct().Count() != numbers.Count)
				{
					offenders.Add($"seed {seed} {team.Definition.Abbr}: two players share a number");
				}
			}

			offenders.AddRange(league.FreeAgents.Where(f => f.JerseyNumber is not null).Select(f => $"seed {seed}: free agent {f.Slot} wears {f.JerseyNumber}"));
		}

		offenders.Take(20).ShouldBeEmpty();

		// Drawn, not handed out in order: across the 640 teams, each position's first player has worn more than one number
		var handedOut =
			from g in Twenty
			from team in g.League.Teams
			from position in team.Players.GroupBy(p => p.Position)
			group position.First().JerseyNumber by position.Key into firsts
			where firsts.Distinct().Count() == 1
			select $"{firsts.Key} always wears {firsts.First()}";

		handedOut.ShouldBeEmpty();
	}

	[Fact]
	public void Every_player_has_a_name_no_teammates_share_one_and_none_is_blocklisted()
	{
		var blocked = new HashSet<string>(Data.FamousPeople, PersonNames.FullNames);
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			offenders.AddRange(Everyone(league)
				.Where(p => p.FirstName.Length == 0 || p.LastName.Length == 0 || blocked.Contains(FullName(p)))
				.Select(p => $"seed {seed}: slot {p.Slot} is \"{FullName(p)}\""));

			offenders.AddRange(
				from team in league.Teams
				from name in team.Players.GroupBy(FullName, PersonNames.FullNames)
				where name.Count() > 1
				select $"seed {seed} {team.Definition.Abbr}: {name.Count()} × {name.Key}");
		}

		offenders.Take(20).ShouldBeEmpty();
	}

	// ---- Determinism ----

	[Fact]
	public void The_same_seed_and_season_give_the_same_league_and_another_seed_or_season_another()
	{
		var generator = new LeagueGenerator(Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System);

		// Seed 2 first, so seed 1 also proves that nothing carries over from one Generate to the next
		var two = generator.Generate(2, Season);
		var one = generator.Generate(1, Season);

		Json(one).ShouldBe(Json(Twenty[0].League));
		Json(two).ShouldBe(Json(Twenty[1].League));

		// Compare what was drawn, slot by slot: the seed and season are fields too, so whole leagues differ even from the same draws
		SameDraws(one, two).ShouldBe(0);
		SameDraws(one, generator.Generate(1, Season + 1)).ShouldBe(0);

		static string Json(GeneratedLeague league) => JsonSerializer.Serialize(league);

		static int SameDraws(GeneratedLeague a, GeneratedLeague b) => Draws(a).Zip(Draws(b)).Count(d => d.First == d.Second);

		static IEnumerable<string> Draws(GeneratedLeague league) => Everyone(league).OrderBy(p => p.Slot).Select(p => JsonSerializer.Serialize(p.Measurables));
	}

	// ---- Distributions ----

	[Fact]
	public void Each_position_and_tier_s_OVR_matches_its_tier_s_target()
	{
		// ±3 on the mean and ±2 on the SD, over the 20 seeds. K, P and LS check only the Starter mean: their OVR is ~90% two
		// kicking skills, so the shared quality draw spreads it wider (SD ~9.5) and the one-deep tiers sit further apart;
		// only ~3 of each are free agents. Closest to the edges on 2026-10-29: Starter QBs (+2.5 on the mean, +1.8 on the
		// SD), Fringe CBs and WRs (+2.5, +2.4) and Backup TEs (−2.4)
		var players = Twenty.SelectMany(g => Everyone(g.League)).Select(p => (p.Position, p.Tier, Ovr: Ovr(p))).ToList();
		var offenders =
			from position in Positions
			from tier in Enum.GetValues<PlayerTier>()
			where !Kicking.Contains(position) || tier == PlayerTier.Starter
			let ovr = players.Where(p => p.Position == position && p.Tier == tier).Select(p => p.Ovr).ToList()
			where ovr.Count > 0
			let target = Generation.Tiers[tier].Target
			let mean = ovr.Average()
			let sd = Math.Sqrt(ovr.Average(x => (x - mean) * (x - mean)))
			where Math.Abs(mean - target.Mean) > 3 || (!Kicking.Contains(position) && Math.Abs(sd - target.Sd) > 2)
			select $"{tier} {position}: mean {mean:F1} and SD {sd:F1} over {ovr.Count}, for a target of {target.Mean} and {target.Sd}";

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Players_at_90_plus_and_the_mean_age_hit_their_targets()
	{
		// The 90+ count is a mean over the seeds because single seeds range wider: 15–30 on 2026-10-29, 21.4 on average
		// (20.9 in the reference run). Each league's mean age is checked: 26.79–27.00
		var targets = Generation.Targets;
		var ninetyPlus = Twenty.Select(g => Everyone(g.League).Count(p => OverallCalculator.Display(Ovr(p)) >= 90)).ToList();

		ninetyPlus.Average().ShouldBeInRange(targets.Ovr90Plus.Min, targets.Ovr90Plus.Max, $"by seed: {string.Join(", ", ninetyPlus)}");

		var ages =
			from g in Twenty
			let age = Everyone(g.League).Average(p => p.Age)
			where Math.Abs(age - targets.MeanAge.Value) > targets.MeanAge.Tolerance
			select $"seed {g.Seed}: a mean age of {age:F2}";

		ages.ShouldBeEmpty();
	}

	// ---- Helpers ----

	private static IEnumerable<GeneratedPlayer> Everyone(GeneratedLeague league) => league.Teams.SelectMany(t => t.Players).Concat(league.FreeAgents);

	private static double Ovr(GeneratedPlayer player) => OverallCalculator.Overall(Profiles[player.Position], player.Ratings);

	private static string FullName(GeneratedPlayer player) => $"{player.FirstName} {player.LastName}";
}
