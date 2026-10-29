using System.Globalization;
using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Tests.Contracts;

/// <summary>
/// The cap trades (M1 WP4 guide §4.4, D-081 #1, F-12 §4.8) on hand-built leagues with a $100 cap, so a team's total
/// reads as its percentage and the band is 85 to 97. A kicker on a minimum deal, which never moves, sets each team's
/// total. No deal is premium in the hand-built leagues except where a test says so, so D-086's guard stays out of the
/// other rules' way; the drawn leagues have it at 10, and each of their trades is checked against D-087's choice of
/// teams. LeagueGeneratorTests checks the band over 20 generated leagues.
/// </summary>
public sealed class CapTradesTests
{
	private const int Season = 2026;

	private static readonly ContractData Band = ShippedSeedData.Instance.Contracts with { Cap = 100, Utilization = new Utilization(0.85, 0.97), PremiumShare = 1 };
	private static readonly ContractData PremiumOver30 = Band with { PremiumShare = 0.30 };
	private static readonly ContractData PremiumOver10 = Band with { PremiumShare = 0.10 };
	private static readonly GeneratedPlayer Template = new PlayerGenerator(ShippedSeedData.Instance).Generate(1, Season, 0, Position.QB, PlayerTier.Starter, 0);
	private static readonly Position[] DrawnPositions = [Position.QB, Position.WR, Position.WR, Position.OT, Position.OT, Position.CB];

	// ---- When it trades ----

	[Theory]
	[InlineData(97, 85, 0)]
	[InlineData(98, 85, 1)]
	[InlineData(97, 84, 1)]
	public void A_league_inside_the_band_makes_no_trade_and_the_band_s_edges_are_inside(long high, long low, int expected)
	{
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Filler(1, high - 50)],
			[Player(100, Position.QB, PlayerTier.Starter, 45), Filler(101, low - 45)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(expected);
	}

	[Fact]
	public void The_trade_evens_the_highest_and_the_lowest_team()
	{
		// 110 and 80: half the spread is 15, exactly the quarterbacks' difference, so both teams end at 95
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Filler(1, 60)],
			[Player(100, Position.QB, PlayerTier.Starter, 35), Filler(101, 45)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);

		Layout(rosters).ShouldBe("100 1 | 0 101");
		Totals(rosters).ShouldBe([95, 95]);
	}

	[Fact]
	public void When_the_highest_and_lowest_teams_can_t_trade_the_widest_pair_that_can_does()
	{
		// D-087. Team 0 (110) has only a quarterback and team 1 (80) only a receiver, so they can't trade with each other.
		// Both are 15 from team 2 (95), and the tie goes to the lower index: team 0's quarterback for team 2's.
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Filler(1, 60)],
			[Player(100, Position.WR, PlayerTier.Starter, 30), Filler(101, 50)],
			[Player(200, Position.QB, PlayerTier.Starter, 42), Player(201, Position.WR, PlayerTier.Starter, 40), Filler(202, 13)]);

		CapTrades.Balance(rosters, Season, Band, maxTrades: 1).ShouldBe(1);
		Layout(rosters).ShouldBe("200 1 | 100 101 | 0 201 202");
	}

	[Fact]
	public void Two_teams_inside_the_band_never_trade_with_each_other()
	{
		// Team 0 (110) has only a tight end and team 1 (80) only a receiver, so neither can trade. Teams 2 (95) and 3 (90)
		// could swap quarterbacks, but the trade would bring no team nearer the band.
		var rosters = League(
			[Player(0, Position.TE, PlayerTier.Starter, 50), Filler(1, 60)],
			[Player(100, Position.WR, PlayerTier.Starter, 30), Filler(101, 50)],
			[Player(200, Position.QB, PlayerTier.Starter, 45), Filler(201, 50)],
			[Player(300, Position.QB, PlayerTier.Starter, 44), Filler(301, 46)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(0);
	}

	[Fact]
	public void The_pair_closest_to_half_the_spread_is_traded()
	{
		// Half the spread is 15: the quarterbacks differ by 14, the receivers by 20
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Player(1, Position.WR, PlayerTier.Starter, 40), Filler(2, 20)],
			[Player(100, Position.QB, PlayerTier.Starter, 36), Player(101, Position.WR, PlayerTier.Starter, 20), Filler(102, 24)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
		Layout(rosters).ShouldBe("100 1 2 | 0 101 102");
	}

	[Theory]
	[InlineData(ContractType.Veteran, ContractType.Veteran, Position.QB, PlayerTier.Starter, true)]
	[InlineData(ContractType.Rookie, ContractType.Veteran, Position.QB, PlayerTier.Starter, false)]
	[InlineData(ContractType.Veteran, ContractType.Rookie, Position.QB, PlayerTier.Starter, false)]
	[InlineData(ContractType.Minimum, ContractType.Veteran, Position.QB, PlayerTier.Starter, false)]
	[InlineData(ContractType.Veteran, ContractType.Minimum, Position.QB, PlayerTier.Starter, false)]
	[InlineData(ContractType.Veteran, ContractType.Veteran, Position.WR, PlayerTier.Starter, false)]
	[InlineData(ContractType.Veteran, ContractType.Veteran, Position.QB, PlayerTier.Backup, false)]
	public void Only_two_veterans_at_the_same_position_and_tier_are_traded(
		ContractType highType, ContractType lowType, Position lowPosition, PlayerTier lowTier, bool closestMoves)
	{
		// The first pair's 15 is exactly half the spread; the tackles' 10 is the eligible one left
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50, highType), Player(1, Position.OT, PlayerTier.Starter, 30), Filler(2, 30)],
			[Player(100, lowPosition, lowTier, 35, lowType), Player(101, Position.OT, PlayerTier.Starter, 20), Filler(102, 25)]);

		CapTrades.Balance(rosters, Season, Band, maxTrades: 1).ShouldBe(1);
		Layout(rosters).ShouldBe(closestMoves ? "100 1 2 | 0 101 102" : "0 101 2 | 100 1 102");
	}

	[Theory]
	[InlineData(45, 30, 1)]
	[InlineData(50, 20, 0)]   // the whole spread: it would only swap which team is high
	[InlineData(55, 20, 0)]   // more than the spread
	[InlineData(40, 40, 0)]   // no difference
	[InlineData(30, 45, 0)]   // the high team's player is the cheaper one
	public void A_trade_must_narrow_the_spread(long highHit, long lowHit, int expected)
	{
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, highHit), Filler(1, 110 - highHit)],
			[Player(100, Position.QB, PlayerTier.Starter, lowHit), Filler(101, 80 - lowHit)]);
		var before = Layout(rosters);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(expected);

		if (expected == 0)
		{
			Layout(rosters).ShouldBe(before);
		}
	}

	[Theory]
	[InlineData(31, 5, 0)]   // the low team would hold the 50 and its own 31
	[InlineData(30, 5, 1)]   // 30 is the line itself, not over it
	[InlineData(5, 31, 0)]   // the high team would hold the 35 and its own 31
	[InlineData(5, 30, 1)]
	public void No_trade_leaves_a_team_with_two_premium_deals_at_one_position(long lowBackup, long highBackup, int expected)
	{
		// D-086, with a hit over 30 premium. The starters' 15 is half the spread, and no other pair may stand in for them.
		// The low team's kicker is over 30 too, which counts for nothing: he's at another position.
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Player(1, Position.QB, PlayerTier.Backup, highBackup), Filler(2, 60 - highBackup)],
			[Player(100, Position.QB, PlayerTier.Starter, 35), Player(101, Position.QB, PlayerTier.Backup, lowBackup), Filler(102, 45 - lowBackup)]);

		CapTrades.Balance(rosters, Season, PremiumOver30).ShouldBe(expected);
	}

	[Fact]
	public void A_team_s_total_counts_this_season_s_cap_hits()
	{
		// The quarterback's deal hits 10 last season, 50 this one and 5 the next. Counting this season, team 0 is at 110
		// and the quarterbacks trade; counting either other season, team 0 would be the low team with nothing to trade.
		var deal = new GeneratedContract(ContractType.Veteran, Season - 1, 65, 0, 0,
			[new(Season - 1, 10, 0, 0, 10), new(Season, 50, 0, 0, 50), new(Season + 1, 5, 0, 0, 5)]);
		var rosters = League(
			[Template with { Slot = 0, Position = Position.QB, Tier = PlayerTier.Starter, Contract = deal }, Filler(1, 60)],
			[Player(100, Position.QB, PlayerTier.Starter, 35), Filler(101, 45)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
	}

	[Fact]
	public void A_rostered_player_without_a_deal_for_this_season_throws()
	{
		var ended = new GeneratedContract(ContractType.Veteran, Season - 2, 20, 0, 0, [new(Season - 2, 10, 0, 0, 10), new(Season - 1, 10, 0, 0, 10)]);

		Should.Throw<InvalidOperationException>(() => CapTrades.Balance(League([Template with { Slot = 0, Contract = null }]), Season, Band));
		Should.Throw<InvalidOperationException>(() => CapTrades.Balance(League([Template with { Slot = 0, Contract = ended }]), Season, Band));
	}

	// ---- Ties ----

	[Fact]
	public void Equal_teams_go_to_the_lower_index_and_max_trades_stops_it()
	{
		// Teams 0 and 1 are both highest, 2 and 3 both lowest: the first trade is between teams 0 and 2, and the cap of
		// one trade stops it with team 1 still at 110. The next call trades 1 with 3.
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Filler(1, 60)],
			[Player(100, Position.QB, PlayerTier.Starter, 50), Filler(101, 60)],
			[Player(200, Position.QB, PlayerTier.Starter, 35), Filler(201, 45)],
			[Player(300, Position.QB, PlayerTier.Starter, 35), Filler(301, 45)]);

		CapTrades.Balance(rosters, Season, Band, maxTrades: 1).ShouldBe(1);
		Layout(rosters).ShouldBe("200 1 | 100 101 | 0 201 | 300 301");

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
		Layout(rosters).ShouldBe("200 1 | 300 101 | 0 201 | 100 301");
	}

	[Fact]
	public void Equal_pairs_go_to_the_high_team_s_lower_slot()
	{
		// Slots 1 and 0 both cost 50; slot 0 is second on the roster and still goes first
		var rosters = League(
			[Player(1, Position.QB, PlayerTier.Starter, 50), Player(0, Position.QB, PlayerTier.Starter, 50), Filler(2, 10)],
			[Player(100, Position.QB, PlayerTier.Starter, 35), Filler(101, 45)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
		Layout(rosters).ShouldBe("1 100 2 | 0 101");
	}

	[Fact]
	public void Equal_pairs_from_one_high_player_go_to_the_low_team_s_lower_slot()
	{
		var rosters = League(
			[Player(0, Position.QB, PlayerTier.Starter, 50), Filler(1, 60)],
			[Player(101, Position.QB, PlayerTier.Starter, 35), Player(100, Position.QB, PlayerTier.Starter, 35), Filler(102, 10)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
		Layout(rosters).ShouldBe("100 1 | 101 0 102");
	}

	[Fact]
	public void The_high_team_s_slot_decides_before_the_low_team_s()
	{
		// Two pairs miss half the spread by nothing: (1, 100) and (0, 101). The high team's lower slot, 0, wins.
		var rosters = League(
			[Player(1, Position.QB, PlayerTier.Starter, 50), Player(0, Position.QB, PlayerTier.Starter, 51), Filler(2, 9)],
			[Player(100, Position.QB, PlayerTier.Starter, 35), Player(101, Position.QB, PlayerTier.Starter, 36), Filler(102, 9)]);

		CapTrades.Balance(rosters, Season, Band).ShouldBe(1);
		Layout(rosters).ShouldBe("1 101 2 | 100 0 102");
	}

	// ---- Over drawn leagues ----

	[Fact]
	public void Every_trade_follows_the_rules_and_evens_the_league()
	{
		var offenders = new List<string>();
		var (balanced, allTrades) = (0, 0);

		for (ulong seed = 1; seed <= 20; seed++)
		{
			var trades = CapTrades.Balance(DrawnLeague(seed), Season, PremiumOver10);
			allTrades += trades;

			// Balance stopped after k trades is the full run's first k (it draws nothing), so each trade can be seen
			var previous = DrawnLeague(seed);

			for (var k = 1; k <= trades; k++)
			{
				var next = DrawnLeague(seed);
				CapTrades.Balance(next, Season, PremiumOver10, maxTrades: k).ShouldBe(k);
				offenders.AddRange(Trade(previous, next).Select(o => $"seed {seed}, trade {k}: {o}"));
				previous = next;
			}

			// It stopped for a reason: every team in the band, no two teams left that can trade, or the cap on trades
			if (Totals(previous).All(t => t is >= 85 and <= 97))
			{
				balanced++;
			}
			else if (trades < 200 && NextTeams(previous) is not null)
			{
				offenders.Add($"seed {seed}: stopped outside the band with two teams left that can trade");
			}
		}

		offenders.Take(20).ShouldBeEmpty();

		// Or the rules above checked nothing
		balanced.ShouldBeGreaterThan(0);
		allTrades.ShouldBeGreaterThan(20);
	}

	// ---- Helpers ----

	private static List<List<GeneratedPlayer>> League(params List<GeneratedPlayer>[] teams) => [.. teams];

	private static GeneratedPlayer Player(int slot, Position position, PlayerTier tier, long hit, ContractType type = ContractType.Veteran) =>
		Template with
		{
			Slot = slot,
			Position = position,
			Tier = tier,
			Contract = new GeneratedContract(type, Season, hit, 0, 0, [new ContractSeason(Season, hit, 0, 0, hit)]),
		};

	/// <summary>A kicker on a minimum deal: never traded, he sets his team's total.</summary>
	private static GeneratedPlayer Filler(int slot, long hit) => Player(slot, Position.K, PlayerTier.Starter, hit, ContractType.Minimum);

	/// <summary>Eight teams of twelve from the seed: mostly veterans, some rookie and minimum deals, hits of 1 to 14.</summary>
	private static List<List<GeneratedPlayer>> DrawnLeague(ulong seed)
	{
		var random = new GfoRandom(seed);
		var rosters = new List<List<GeneratedPlayer>>();

		for (var t = 0; t < 8; t++)
		{
			var roster = new List<GeneratedPlayer>();

			for (var i = 0; i < 12; i++)
			{
				var hit = random.Next(1, 15);
				var type = random.Next(0, 20) switch { < 3 => ContractType.Rookie, < 5 => ContractType.Minimum, _ => ContractType.Veteran };
				roster.Add(Player(t * 12 + i, DrawnPositions[i % 6], i < 6 ? PlayerTier.Starter : PlayerTier.Backup, hit, type));
			}

			rosters.Add(roster);
		}

		return rosters;
	}

	private static long Hit(GeneratedPlayer player) => player.Contract!.Seasons.Single(s => s.Season == Season).CapHit;

	private static long[] Totals(List<List<GeneratedPlayer>> rosters) => [.. rosters.Select(r => r.Sum(Hit))];

	private static string Layout(List<List<GeneratedPlayer>> rosters) =>
		string.Join(" | ", rosters.Select(r => string.Join(" ", r.Select(p => p.Slot.ToString(CultureInfo.InvariantCulture)))));

	/// <summary>
	/// D-087: the two teams the next trade is between. The furthest apart that have a pair the rules allow, where the
	/// higher is over the band or the lower under it, ties to the lower indexes. Null when there are none.
	/// </summary>
	private static (int Hi, int Lo)? NextTeams(List<List<GeneratedPlayer>> rosters)
	{
		var totals = Totals(rosters);
		var teams = Enumerable.Range(0, totals.Length).ToList();

		return (
			from hi in teams
			from lo in teams
			where totals[hi] > totals[lo] && (totals[hi] > 97 || totals[lo] < 85)
			orderby totals[hi] - totals[lo] descending, hi, lo
			where Eligible(rosters, hi, lo).Any()
			select ((int Hi, int Lo)?)(hi, lo)).FirstOrDefault();
	}

	/// <summary>The pairs the rules allow from team <paramref name="hi"/> to team <paramref name="lo"/>, with a hit over 10 premium (the drawn leagues' hits run 1 to 14).</summary>
	private static IEnumerable<(GeneratedPlayer Leaving, GeneratedPlayer Arriving)> Eligible(List<List<GeneratedPlayer>> rosters, int hi, int lo)
	{
		var premium = PremiumOver10.PremiumShare * PremiumOver10.Cap;
		var totals = Totals(rosters);

		return
			from leaving in rosters[hi]
			from arriving in rosters[lo]
			let difference = Hit(leaving) - Hit(arriving)
			where leaving.Contract!.Type == ContractType.Veteran && arriving.Contract!.Type == ContractType.Veteran
				&& leaving.Position == arriving.Position && leaving.Tier == arriving.Tier
				&& difference > 0 && difference < totals[hi] - totals[lo]
				&& !Doubles(rosters[lo], leaving, arriving) && !Doubles(rosters[hi], arriving, leaving)
			select (leaving, arriving);

		// D-086: the team gives up one player for the other and then holds two premium deals at his position
		bool Doubles(List<GeneratedPlayer> team, GeneratedPlayer gaining, GeneratedPlayer losing) =>
			Hit(gaining) > premium && team.Any(p => p.Slot != losing.Slot && p.Position == gaining.Position && Hit(p) > premium);
	}

	/// <summary>
	/// What one trade broke, from the league before it to the league after: one place changed on each of the two teams
	/// the rules pick, the two players swapped, a pair the rules allow, and a league more even than before. The two
	/// leagues are separate draws of the same seed, so players are matched by slot.
	/// </summary>
	private static IEnumerable<string> Trade(List<List<GeneratedPlayer>> before, List<List<GeneratedPlayer>> after)
	{
		var totals = Totals(before);

		if (NextTeams(before) is not { } teams)
		{
			yield return "a trade when no two teams could make one";
			yield break;
		}

		var (hi, lo) = teams;
		var changed = Enumerable.Range(0, before.Count)
			.SelectMany(t => Enumerable.Range(0, before[t].Count).Where(i => before[t][i].Slot != after[t][i].Slot).Select(i => (Team: t, Index: i)))
			.ToList();

		if (changed.Count != 2 || changed.Count(c => c.Team == hi) != 1 || changed.Count(c => c.Team == lo) != 1)
		{
			yield return $"changed {string.Join(", ", changed)}, not one place on team {hi} and one on team {lo}";
			yield break;
		}

		var h = changed.Single(c => c.Team == hi).Index;
		var l = changed.Single(c => c.Team == lo).Index;
		var (leaving, arriving) = (before[hi][h], before[lo][l]);

		if (after[hi][h].Slot != arriving.Slot || after[lo][l].Slot != leaving.Slot)
		{
			yield return "the two players weren't swapped";
		}

		if (!Eligible(before, hi, lo).Any(p => p.Leaving.Slot == leaving.Slot && p.Arriving.Slot == arriving.Slot))
		{
			yield return $"slots {leaving.Slot} and {arriving.Slot} aren't a pair the rules allow";
		}

		// The league's mean never moves, so a smaller sum of squares is a more even league
		if (Totals(after).Sum(t => t * t) >= totals.Sum(t => t * t))
		{
			yield return "the league didn't get more even";
		}
	}
}
