using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Tests.Leagues;

/// <summary>
/// Contracts and the cap trades across whole leagues (M1 WP4 guide §5, slice 4's rows), on the shared seeds 1–20.
/// <see cref="LeagueShapeTests"/> has slice 3's rows; the golden league waits on the choice of snapshot tool.
/// </summary>
public sealed class LeagueGeneratorTests
{
	private const int Season = GeneratedLeagues.Season;

	private static readonly ISeedData Data = ShippedSeedData.Instance;
	private static readonly ContractData Contracts = Data.Contracts;
	private static readonly int Active = Data.Generation.Roster.Active;
	private static readonly Dictionary<Position, PositionProfile> Profiles = Data.Positions.Positions.ToDictionary(p => p.Position);

	private static IReadOnlyList<LoggedLeague> Twenty => GeneratedLeagues.Twenty;

	// ---- Contracts ----

	[Fact]
	public void Every_rostered_player_has_the_deal_his_row_of_the_table_gives_and_no_free_agent_has_one()
	{
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			offenders.AddRange(league.FreeAgents.Where(p => p.Contract is not null).Select(p => $"seed {seed}: free agent {p.Slot} has a contract"));

			foreach (var player in league.Teams.SelectMany(t => t.Players))
			{
				var label = $"seed {seed}, slot {player.Slot} ({player.Position}, experience {player.Experience}, {(player.Draft is null ? "undrafted" : "drafted")})";
				offenders.AddRange(Broken(player).Select(b => $"{label}: {b}"));
			}
		}

		offenders.Distinct().Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void A_veteran_deal_s_length_fits_his_league_wide_contract_tier_and_age()
	{
		var offenders = new List<string>();
		var (starters, backups) = (0, 0);

		foreach (var (seed, league, _) in Twenty)
		{
			var tiers = ContractTiers(league);

			foreach (var player in league.Teams.SelectMany(t => t.Players).Where(p => p.Contract!.Type == ContractType.Veteran))
			{
				var tier = tiers[player.Slot];
				var band = Contracts.Veterans.Lengths[tier].First(b => player.Age <= b.ThroughAge);
				var years = player.Contract!.Seasons.Count;

				if (tier == PlayerTier.Starter)
				{
					starters++;
				}
				else
				{
					backups++;
				}

				if (years < band.Min || years > band.Max)
				{
					offenders.Add($"seed {seed}, slot {player.Slot}: a {tier} at {player.Age} signed for {years}, not {band.Min}–{band.Max}");
				}
			}
		}

		offenders.Take(20).ShouldBeEmpty();
		starters.ShouldBeGreaterThan(0);
		backups.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void Pay_follows_the_league_wide_rank_at_every_position()
	{
		// Veteran deals only: rookie and minimum deals don't key on rank. Across the 20 leagues, the rank correlation of
		// OVR and APY is 0.982–0.992 at every position whose max is at least 5 × the top minimum. At FB, K, P and LS the
		// max ($1.5M–$5.5M) sits so close to the minimums that the experience-based minimum and the ±20% wiggle outweigh
		// rank (0.90, 0.92, 0.87 and 0.08 on 2026-10-28), so they're left out.
		var topMinimum = Contracts.MinimumSalaries.Max(m => m.Salary);
		var offenders =
			from position in Enum.GetValues<Position>()
			where Contracts.Veterans.MaxApy[position] >= 5 * topMinimum
			let pairs = Twenty
				.SelectMany(g => g.League.Teams.SelectMany(t => t.Players))
				.Where(p => p.Position == position && p.Contract!.Type == ContractType.Veteran)
				.Select(p => (Ovr: OverallCalculator.Overall(Profiles[position], p.Ratings), Apy: p.Contract!.TotalValue / (double)p.Contract.Seasons.Count))
				.ToList()
			let correlation = GeneratedLeagues.Spearman(pairs)
			where correlation < 0.95
			select $"{position}: {correlation:F3} over {pairs.Count} veteran deals";

		offenders.ShouldBeEmpty();
	}

	// ---- The cap trades ----

	[Fact]
	public void Every_team_ends_inside_the_cap_band_with_one_1301_and_no_1302()
	{
		var offenders = new List<string>();

		foreach (var (seed, league, events) in Twenty)
		{
			foreach (var team in league.Teams)
			{
				var total = Total(team.Players);

				if (total < Contracts.Utilization.Min * Contracts.Cap || total > Contracts.Utilization.Max * Contracts.Cap)
				{
					offenders.Add($"seed {seed} {team.Definition.Abbr}: {Percent(total)}% of the cap");
				}
			}

			if (events.Count(e => e.EventID == 1301) != 1 || events.Any(e => e.EventID == 1302))
			{
				offenders.Add($"seed {seed}: {events.Count(e => e.EventID == 1301)} × 1301, {events.Count(e => e.EventID == 1302)} × 1302");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Every_traded_player_is_a_veteran_in_the_place_of_one_at_his_position_and_tier()
	{
		var offenders = new List<string>();
		var traded = 0;

		foreach (var (seed, league, _) in Twenty)
		{
			for (var t = 0; t < league.Teams.Count; t++)
			{
				var players = league.Teams[t].Players;

				// Step 3's layout survives the trades: positions in enum order, each one's first roster.starters its Starters
				var positions = players.Select(p => p.Position).ToList();
				if (players.Count != Active || !positions.SequenceEqual(positions.Order()))
				{
					offenders.Add($"seed {seed} team {t}: {players.Count} players, positions out of order");
				}

				foreach (var group in players.GroupBy(p => p.Position))
				{
					var starters = Profiles[group.Key].Roster.Starters;
					if (!group.Select((p, i) => p.Tier == (i < starters ? PlayerTier.Starter : PlayerTier.Backup)).All(x => x))
					{
						offenders.Add($"seed {seed} team {t} {group.Key}: tiers {string.Join(",", group.Select(p => p.Tier))}");
					}
				}

				// A slot says the team a player was generated for, so anyone elsewhere was traded
				foreach (var player in players.Where(p => p.Slot / Active != t))
				{
					traded++;
					if (player.Contract!.Type != ContractType.Veteran)
					{
						offenders.Add($"seed {seed}: slot {player.Slot} on a {player.Contract.Type} deal moved to team {t}");
					}
				}
			}
		}

		offenders.Take(20).ShouldBeEmpty();
		traded.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void Event_1301_reports_the_trades_and_the_teams_shares_of_the_cap()
	{
		var offenders = new List<string>();

		foreach (var (seed, league, events) in Twenty)
		{
			var fitted = events.Single(e => e.EventID == 1301).Values;
			var totals = league.Teams.Select(t => Total(t.Players)).ToList();
			var moved = league.Teams.Select((team, t) => team.Players.Count(p => p.Slot / Active != t)).Sum();
			var expected = (Percent(totals.Min()), Percent(totals.Max()), Percent(totals.Average()));
			var logged = ((double)fitted["MinPercent"]!, (double)fitted["MaxPercent"]!, (double)fitted["MeanPercent"]!);

			if (logged != expected)
			{
				offenders.Add($"seed {seed}: logged {logged}, the league is at {expected}");
			}

			// Each trade moves two players (one can later come home), so no more than twice the count are away
			if (moved > 2 * (int)fitted["TradeCount"]!)
			{
				offenders.Add($"seed {seed}: {moved} players away from home after {fitted["TradeCount"]} trades");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void A_band_no_trade_can_reach_logs_a_1302_for_every_team()
	{
		// 10–11% of the cap: every generated team is far above it
		var generated = GeneratedLeagues.Generate(new SeedDataWith(Data, Contracts with { Utilization = new Utilization(0.10, 0.11) }), 42);
		var warnings = generated.Events.Where(e => e.EventID == 1302).ToList();
		var teams = generated.League.Teams;

		warnings.Select(w => w.Level).Distinct().ShouldBe([LogLevel.Warning]);
		warnings.Select(w => (string)w.Values["Team"]!).ShouldBe(teams.Select(t => t.Definition.Abbr));
		warnings.Select(w => (double)w.Values["Percent"]!).ShouldBe(teams.Select(t => Percent(Total(t.Players))));
		generated.Events.Count(e => e.EventID == 1301).ShouldBe(1);
	}

	// ---- Helpers ----

	private static long Total(IEnumerable<GeneratedPlayer> players) => players.Sum(p => p.Contract!.Seasons.Single(s => s.Season == Season).CapHit);

	/// <summary>A total as events 1301–1302 show it: a percentage of the cap, to one decimal.</summary>
	private static double Percent(double total) => Math.Round(total * 100 / Contracts.Cap, 1, MidpointRounding.AwayFromZero);

	/// <summary>
	/// F-12 §4.7's contract tier, from scratch: per position, every rostered player from worst to best by OVR, then slot,
	/// and the best teams × starters are Starters.
	/// </summary>
	private static Dictionary<int, PlayerTier> ContractTiers(GeneratedLeague league)
	{
		var tiers = new Dictionary<int, PlayerTier>();

		foreach (var position in league.Teams.SelectMany(t => t.Players).GroupBy(p => p.Position))
		{
			var ordered = position.OrderBy(p => OverallCalculator.Overall(Profiles[position.Key], p.Ratings)).ThenBy(p => p.Slot).ToList();
			var firstStarter = ordered.Count - league.Teams.Count * Profiles[position.Key].Roster.Starters;

			for (var i = 0; i < ordered.Count; i++)
			{
				tiers[ordered[i].Slot] = i >= firstStarter ? PlayerTier.Starter : PlayerTier.Backup;
			}
		}

		return tiers;
	}

	/// <summary>What breaks the guide's §4.4 table, or what every deal keeps at creation.</summary>
	private static IEnumerable<string> Broken(GeneratedPlayer player)
	{
		if (player.Contract is not { } contract)
		{
			yield return "no contract";
			yield break;
		}

		var years = contract.Seasons.Count;
		var row = player switch
		{
			{ Draft: not null } when player.Experience < Contracts.RookieScale.Years => "rookie",
			{ Draft: null } when player.Experience < Contracts.Undrafted.Years => "undrafted",
			_ => "veteran or minimum",
		};
		var fits = row switch
		{
			"rookie" => contract.Type == ContractType.Rookie && contract.SignedSeason == player.Draft!.Season && years == Contracts.RookieScale.Years,
			"undrafted" => contract.Type == ContractType.Minimum && contract.SignedSeason == Season - player.Experience && years == Contracts.Undrafted.Years,
			_ => contract.Type == ContractType.Veteran || (contract.Type == ContractType.Minimum && contract.SignedSeason == Season && years == 1),
		};

		if (!fits)
		{
			yield return $"a {row} deal expected, but it's {contract.Type}, signed {contract.SignedSeason} for {years}";
		}

		// Never signed before his entry, and this season or a later one left
		var elapsed = Season - contract.SignedSeason;
		if (elapsed < 0 || elapsed > player.Experience || elapsed >= years)
		{
			yield return $"{elapsed} of {years} seasons elapsed";
		}

		for (var i = 0; i < years; i++)
		{
			if (contract.Seasons[i].CapHit != CapMath.CapHit(contract.Seasons[i].BaseSalary, contract.SigningBonus, years, i))
			{
				yield return $"season {i}: the cap hit isn't CapMath's";
			}
		}
	}

	/// <summary>The shipped data with other contract rules.</summary>
	private sealed class SeedDataWith(ISeedData data, ContractData contracts) : ISeedData
	{
		public TeamSet TeamSet => data.TeamSet;
		public CollegeList Colleges => data.Colleges;
		public PositionData Positions => data.Positions;
		public GenerationSettings Generation => data.Generation;
		public JerseyRules JerseyRules => data.JerseyRules;
		public NameList FirstNames => data.FirstNames;
		public NameList LastNames => data.LastNames;
		public HometownList Hometowns => data.Hometowns;
		public IReadOnlyList<string> FamousPeople => data.FamousPeople;
		public ContractData Contracts => contracts;
	}
}
