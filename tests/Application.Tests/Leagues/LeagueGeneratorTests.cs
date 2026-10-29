using System.Text.Json;
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

	[Theory]
	[InlineData(311, false)]   // the highest and lowest teams ran out of pairs after 24 trades
	[InlineData(463, true)]    // the whole league's pay came to 84.7% of the cap
	public void The_two_leagues_that_once_missed_the_band_fit_it(ulong seed, bool scaled)
	{
		// D-087's two fixes, on the two leagues of 500 seeds that missed the band before them (2026-10-29)
		var (_, league, events) = GeneratedLeagues.Generate(Data, seed);

		league.Teams.Where(t => !Inside(Total(t.Players), Contracts.Utilization)).Select(t => $"{t.Definition.Abbr} at {Percent(Total(t.Players))}%").ShouldBeEmpty();
		events.Count(e => e.EventID == 1302).ShouldBe(0);
		events.Count(e => e.EventID == 1303).ShouldBe(scaled ? 1 : 0);
	}

	[Theory]
	[InlineData(1.05, 1.15)]   // every league's pay is under this band
	[InlineData(0.70, 0.80)]   // and over this one
	public void A_league_whose_pay_is_outside_the_band_has_its_veteran_deals_scaled_to_the_middle(double min, double max)
	{
		// D-087's fallback, forced by moving the band: seed 1's league, against the shared one made with the shipped band
		var band = new Utilization(min, max);
		var (_, league, events) = GeneratedLeagues.Generate(new SeedDataWith(Data, Contracts with { Utilization = band }), 1);
		var shipped = Twenty[0].League;
		var scaled = events.Single(e => e.EventID == 1303).Values;
		var mean = league.Teams.Average(t => Total(t.Players));

		// Up for a band above the league's pay, down for one below, and the middle reached within a point. The trades
		// never change the league's total, so the mean after them is the scaled one.
		var factor = (double)scaled["Factor"]!;
		(factor > 1).ShouldBe(min > 0.9);
		Math.Abs(mean / Contracts.Cap - (min + max) / 2).ShouldBeLessThan(0.01);
		league.Teams.Where(t => !Inside(Total(t.Players), band)).Select(t => t.Definition.Abbr).ShouldBeEmpty();
		events.Count(e => e.EventID == 1302).ShouldBe(0);

		// Only the veteran deals change, by the factor: the rookie and undrafted ones are the shipped league's, slot for slot
		Deals(league, veteran: false).ShouldBe(Deals(shipped, veteran: false));
		(Pay(league) / (double)Pay(shipped)).ShouldBe(factor, 0.03);

		static List<string> Deals(GeneratedLeague league, bool veteran) =>
			[.. league.Teams.SelectMany(t => t.Players).Where(p => VeteranDeal(p) == veteran).OrderBy(p => p.Slot).Select(p => $"{p.Slot}: {JsonSerializer.Serialize(p.Contract)}")];

		static long Pay(GeneratedLeague league) => league.Teams.SelectMany(t => t.Players).Where(VeteranDeal).Sum(p => p.Contract!.TotalValue);

		// §4.4's table: anyone past his rookie or undrafted deal gets a veteran one, which may come back as a minimum deal
		static bool VeteranDeal(GeneratedPlayer p) => p.Experience >= (p.Draft is null ? Contracts.Undrafted.Years : Contracts.RookieScale.Years);
	}

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

			// The shipped band fits every one of these leagues' pay, so none is scaled (1303)
			if (events.Count(e => e.EventID == 1301) != 1 || events.Any(e => e.EventID is 1302 or 1303))
			{
				offenders.Add($"seed {seed}: {events.Count(e => e.EventID == 1301)} × 1301, {events.Count(e => e.EventID == 1302)} × 1302, {events.Count(e => e.EventID == 1303)} × 1303");
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
	public void No_trade_puts_a_premium_deal_beside_another_at_its_position()
	{
		// D-086: a cap hit over premiumShare of the cap ($30M) is a premium deal. A team can be generated with two (the
		// trades usually move one away), but no trade may bring one in beside another. A player is traded in when his slot
		// is another team's; one who left and came home counts as home, and couldn't have come back beside another either.
		var premium = Contracts.PremiumShare * Contracts.Cap;
		var offenders = new List<string>();

		foreach (var (seed, league, _) in Twenty)
		{
			for (var t = 0; t < league.Teams.Count; t++)
			{
				foreach (var position in league.Teams[t].Players.GroupBy(p => p.Position))
				{
					var deals = position.Where(p => Hit(p) > premium).ToList();

					if (deals.Count > 1 && deals.Any(p => p.Slot / Active != t))
					{
						offenders.Add($"seed {seed} {league.Teams[t].Definition.Abbr} {position.Key}: {string.Join(" and ", deals.Select(p => $"slot {p.Slot} at ${Hit(p) / 1e6:F1}M"))}");
					}
				}
			}
		}

		offenders.ShouldBeEmpty();
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

	private static long Hit(GeneratedPlayer player) => player.Contract!.Seasons.Single(s => s.Season == Season).CapHit;

	private static long Total(IEnumerable<GeneratedPlayer> players) => players.Sum(Hit);

	private static bool Inside(long total, Utilization band) => total >= band.Min * Contracts.Cap && total <= band.Max * Contracts.Cap;

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
