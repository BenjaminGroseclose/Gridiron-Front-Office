using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Tests.Contracts;

/// <summary>
/// The contract generator (M1 WP4 guide §4.4, F-12 §4.4–4.8). The worked examples run on <see cref="Fixture"/>, which
/// writes out F-12's numbers, so tuning contracts.json never breaks the arithmetic. The rules over every pick and over a
/// fixed sample of veteran draws run on the shipped data.
/// </summary>
public sealed class ContractGeneratorTests
{
	private const int Season = 2026;

	/// <summary>F-12 §4.4–4.7's numbers: the whole minimum ladder, rounds 1–2 of the rookie scale and the veteran model.</summary>
	private static readonly ContractData Fixture = new(
		Cap: 300_000_000,
		Utilization: new Utilization(0.85, 0.97),
		PremiumShare: 0.10,
		MinimumSalaries: [new(0, 885_000), new(1, 1_005_000), new(2, 1_075_000), new(3, 1_145_000), new(4, 1_215_000), new(7, 1_300_000)],
		RookieScale: new RookieScale(4, [0.10, 0.20, 0.30, 0.40], [new(1, 54_400_000, 16_100_000, 0.60, 1.00), new(2, 11_000_000, 6_500_000, 0.30, 0.75)]),
		Undrafted: new UndraftedDeal(3, 20_000),
		Veterans: new VeteranDeals(
			Curve: 2.5,
			Wiggle: new Wiggle(0.08, 0.20),
			MaxApy: new Dictionary<Position, long> { [Position.QB] = 45_000_000 },
			Lengths: new Dictionary<PlayerTier, IReadOnlyList<LengthBand>>
			{
				[PlayerTier.Starter] = [new(26, 4, 5), new(29, 3, 4), new(32, 2, 3), new(99, 1, 2)],
				[PlayerTier.Backup] = [new(26, 2, 3), new(29, 1, 2), new(99, 1, 1)],
			},
			BonusShare: new Dictionary<PlayerTier, Share> { [PlayerTier.Starter] = new(0.25, 0.40), [PlayerTier.Backup] = new(0, 0.15) },
			GuaranteeShare: new Dictionary<PlayerTier, Share> { [PlayerTier.Starter] = new(0.45, 0.65), [PlayerTier.Backup] = new(0.10, 0.40) },
			BaseEscalation: 0.08));

	private static readonly ContractGenerator Worked = new(Fixture, 32);

	private static readonly ContractData ShippedContracts = ShippedSeedData.Instance.Contracts;
	private static readonly int Teams = ShippedSeedData.Instance.TeamSet.Teams.Count;
	private static readonly ContractGenerator Shipped = new(ShippedContracts, Teams);
	private static readonly TeamDefinition Team = ShippedSeedData.Instance.TeamSet.Teams[0];

	// The veteran sample's grid
	private static readonly PlayerTier[] ContractTiers = [PlayerTier.Starter, PlayerTier.Backup];
	private static readonly int[] SampleAges = [24, 28, 31, 34];
	private static readonly int[] SampleExperience = [3, 5, 7, 10];
	private static readonly double[] SampleRanks = [0, 0.25, 0.5, 0.75, 0.9, 1];

	// ---- Minimum ----

	[Theory]
	[InlineData(0, 885_000)]
	[InlineData(3, 1_145_000)]
	[InlineData(4, 1_215_000)]
	[InlineData(6, 1_215_000)]
	[InlineData(7, 1_300_000)]
	[InlineData(15, 1_300_000)]
	[InlineData(-1, 885_000)]
	public void The_minimum_is_the_last_step_at_or_below_his_credited_seasons(int creditedSeasons, long expected)
	{
		Worked.Minimum(creditedSeasons).ShouldBe(expected);
	}

	// ---- Rookie deals ----

	[Fact]
	public void The_first_pick_is_paid_as_F_12_works_it_out()
	{
		var contract = Worked.Rookie(new DraftInfo(2024, 1, 1, Team));

		// $54.4M: a 60% bonus prorated over four seasons; the minimums plus 10/20/30/40% of the rest; all guaranteed
		ContractSeason[] seasons =
		[
			new(2024, 2_650_000, 8_160_000, 2_650_000, 10_810_000),
			new(2025, 4_535_000, 8_160_000, 4_535_000, 12_695_000),
			new(2026, 6_370_000, 8_160_000, 6_370_000, 14_530_000),
			new(2027, 8_205_000, 8_160_000, 8_205_000, 16_365_000),
		];

		contract.Type.ShouldBe(ContractType.Rookie);
		contract.SignedSeason.ShouldBe(2024);
		contract.TotalValue.ShouldBe(54_400_000);
		contract.SigningBonus.ShouldBe(32_640_000);
		contract.GuaranteedTotal.ShouldBe(54_400_000);
		contract.Seasons.ShouldBe(seasons);
	}

	[Theory]
	[InlineData(1, 54_400_000)]
	[InlineData(2, 53_164_516)]   // 54.4M − 38.3M / 31 is 53,164,516.13: rounded once, not truncated to ...517
	[InlineData(16, 35_867_742)]
	[InlineData(32, 16_100_000)]
	public void A_first_round_pick_is_on_the_straight_line_rounded_once(int pick, long expected)
	{
		Worked.Rookie(new DraftInfo(2024, 1, pick, Team)).TotalValue.ShouldBe(expected);
	}

	[Fact]
	public void A_second_rounder_s_guarantee_covers_his_early_seasons_and_stops()
	{
		var contract = Worked.Rookie(new DraftInfo(2024, 2, 33, Team));

		// $11M, a 30% bonus, 75% guaranteed: the bonus, then the bases from the first season until $8.25M is reached
		ContractSeason[] seasons =
		[
			new(2024, 1_244_000, 825_000, 1_244_000, 2_069_000),
			new(2025, 1_723_000, 825_000, 1_723_000, 2_548_000),
			new(2026, 2_152_000, 825_000, 1_983_000, 2_977_000),
			new(2027, 2_581_000, 825_000, 0, 3_406_000),
		];

		contract.TotalValue.ShouldBe(11_000_000);
		contract.SigningBonus.ShouldBe(3_300_000);
		contract.GuaranteedTotal.ShouldBe(8_250_000);
		contract.Seasons.ShouldBe(seasons);
	}

	[Fact]
	public void Every_round_runs_in_a_straight_line_from_its_first_pick_to_its_last()
	{
		var offenders = new List<string>();

		foreach (var row in ShippedContracts.RookieScale.Rounds)
		{
			var totals = Enumerable.Range(1, Teams).Select(k => Shipped.Rookie(Draft(row.Round, (row.Round - 1) * Teams + k)).TotalValue).ToList();
			var steps = totals.Zip(totals.Skip(1), (a, b) => b - a).ToList();

			if (totals[0] != row.FirstPick || totals[^1] != row.LastPick)
			{
				offenders.Add($"round {row.Round}: {totals[0]:N0} to {totals[^1]:N0}, not {row.FirstPick:N0} to {row.LastPick:N0}");
			}

			// Even steps, give or take the rounding to whole dollars
			if (steps.Max() - steps.Min() > 1)
			{
				offenders.Add($"round {row.Round}: steps from {steps.Min():N0} to {steps.Max():N0}");
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void Every_pick_s_deal_keeps_the_rookie_rules()
	{
		var scale = ShippedContracts.RookieScale;
		var offenders = new List<string>();

		for (var pick = 1; pick <= scale.Rounds.Count * Teams; pick++)
		{
			var round = scale.Rounds[(pick - 1) / Teams];
			var draft = Draft(round.Round, pick);
			var contract = Shipped.Rookie(draft);
			var bases = contract.Seasons.Select(s => s.BaseSalary).ToList();

			var broken = Broken(contract).ToList();
			if (contract.Type != ContractType.Rookie || contract.SignedSeason != draft.Season || bases.Count != scale.Years)
			{
				broken.Add($"{contract.Type}, signed {contract.SignedSeason}, {bases.Count} seasons");
			}

			if (bases.Where((b, i) => b < Shipped.Minimum(i)).Any())
			{
				broken.Add("a base under his minimum that season");
			}

			if (bases.Zip(bases.Skip(1)).Any(b => b.Second < b.First))
			{
				broken.Add("a base that drops");
			}

			if (contract.SigningBonus != Dollars(contract.TotalValue * round.BonusShare))
			{
				broken.Add($"bonus {contract.SigningBonus:N0}");
			}

			if (contract.GuaranteedTotal != Guaranteed(contract, round.GuaranteeShare))
			{
				broken.Add($"guaranteed {contract.GuaranteedTotal:N0}");
			}

			offenders.AddRange(broken.Select(b => $"pick {pick}: {b}"));
		}

		offenders.ShouldBeEmpty();
	}

	// ---- Undrafted deals ----

	[Fact]
	public void An_undrafted_deal_is_three_minimums_and_a_20000_bonus_from_his_entry_season()
	{
		var contract = Worked.Undrafted(2, Season);

		// The $20,000 bonus prorates 6,668 + 6,666 + 6,666: CapMath puts the remainder in the first season
		ContractSeason[] seasons =
		[
			new(2024, 885_000, 6_668, 0, 891_668),
			new(2025, 1_005_000, 6_666, 0, 1_011_666),
			new(2026, 1_075_000, 6_666, 0, 1_081_666),
		];

		contract.Type.ShouldBe(ContractType.Minimum);
		contract.SignedSeason.ShouldBe(2024);
		contract.TotalValue.ShouldBe(2_985_000);
		contract.SigningBonus.ShouldBe(20_000);
		contract.GuaranteedTotal.ShouldBe(20_000);
		contract.Seasons.ShouldBe(seasons);
	}

	// ---- Veteran terms ----

	[Theory]
	[InlineData(1UL, Position.QB, 0.9, PlayerTier.Starter, 27, 6)]
	[InlineData(2UL, Position.LS, 0.1, PlayerTier.Backup, 31, 9)]
	[InlineData(3UL, Position.CB, 0.5, PlayerTier.Starter, 24, 2)]
	[InlineData(4UL, Position.EDGE, 1.0, PlayerTier.Starter, 34, 12)]
	public void The_terms_are_drawn_in_the_documented_order(ulong seed, Position position, double rank, PlayerTier tier, int age, int experience)
	{
		// The guide's order, drawn by hand: the wiggle, the years, the elapsed seasons, the bonus share, the guarantee share
		var veterans = ShippedContracts.Veterans;
		var random = new GfoRandom(seed);
		var wiggle = Math.Clamp(random.Normal(0, veterans.Wiggle.Sd), -veterans.Wiggle.Max, veterans.Wiggle.Max);
		var band = veterans.Lengths[tier].First(b => age <= b.ThroughAge);
		var years = random.Next(band.Min, band.Max + 1);
		var elapsed = random.Next(0, Math.Min(years - 1, experience) + 1);
		var bonusShare = veterans.BonusShare[tier].Min + random.NextDouble() * (veterans.BonusShare[tier].Max - veterans.BonusShare[tier].Min);
		var guaranteeShare = veterans.GuaranteeShare[tier].Min + random.NextDouble() * (veterans.GuaranteeShare[tier].Max - veterans.GuaranteeShare[tier].Min);
		var minimum = Shipped.Minimum(experience);
		var apy = Dollars((minimum + (veterans.MaxApy[position] - minimum) * Math.Pow(rank, veterans.Curve)) * (1 + wiggle));

		Shipped.DrawVeteranTerms(new GfoRandom(seed), position, rank, tier, age, experience)
			.ShouldBe(new VeteranTerms(apy, years, elapsed, bonusShare, guaranteeShare));
	}

	[Fact]
	public void Every_draw_stays_in_its_bands_and_reaches_both_ends_of_every_length_band()
	{
		var veterans = ShippedContracts.Veterans;
		var offenders = new List<string>();
		var lengths = new HashSet<(PlayerTier, int ThroughAge, int Years)>();
		var elapsedAtMost = 0;

		foreach (var tier in new[] { PlayerTier.Starter, PlayerTier.Backup })
		{
			for (var age = 22; age <= 38; age++)
			{
				for (var experience = 0; experience <= 12; experience++)
				{
					for (ulong seed = 0; seed < 20; seed++)
					{
						var terms = Shipped.DrawVeteranTerms(new GfoRandom(seed), Position.WR, 0.5, tier, age, experience);
						var band = veterans.Lengths[tier].First(b => age <= b.ThroughAge);
						lengths.Add((tier, band.ThroughAge, terms.Years));

						if (terms.Years < band.Min || terms.Years > band.Max)
						{
							offenders.Add($"{tier} at {age}: {terms.Years} years, not {band.Min}–{band.Max}");
						}

						// Never before his entry, and always with this season or a later one left
						if (terms.Elapsed < 0 || terms.Elapsed > Math.Min(terms.Years - 1, experience))
						{
							offenders.Add($"{tier} at {age}, experience {experience}: {terms.Elapsed} of {terms.Years} seasons elapsed");
						}

						if (terms.Elapsed == Math.Min(terms.Years - 1, experience) && terms.Elapsed > 0)
						{
							elapsedAtMost++;
						}

						if (!Within(terms.BonusShare, veterans.BonusShare[tier]) || !Within(terms.GuaranteeShare, veterans.GuaranteeShare[tier]))
						{
							offenders.Add($"{tier}: bonus share {terms.BonusShare}, guarantee share {terms.GuaranteeShare}");
						}
					}
				}
			}
		}

		var missing =
			from tier in veterans.Lengths.Keys
			from band in veterans.Lengths[tier]
			from years in new[] { band.Min, band.Max }
			where !lengths.Contains((tier, band.ThroughAge, years))
			select $"{tier} through {band.ThroughAge}: never {years} years";

		offenders.Concat(missing).Distinct().Take(20).ShouldBeEmpty();
		elapsedAtMost.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void Pay_follows_rank_with_the_same_wiggle_at_every_rank()
	{
		var veterans = ShippedContracts.Veterans;
		var offenders = new List<string>();
		var ranks = Enumerable.Range(0, 21).Select(i => i / 20.0).ToList();

		foreach (var position in Enum.GetValues<Position>())
		{
			for (ulong seed = 0; seed < 20; seed++)
			{
				var terms = ranks.Select(r => Shipped.DrawVeteranTerms(new GfoRandom(seed), position, r, PlayerTier.Starter, 28, 6)).ToList();

				if (terms.Zip(terms.Skip(1)).Any(t => t.Second.Apy < t.First.Apy))
				{
					offenders.Add($"{position}, seed {seed}: APY drops as the rank rises");
				}

				// From his minimum at the bottom of the curve to the position's max at the top, both times the same wiggle
				var minimum = Shipped.Minimum(6);
				var bottom = terms[0].Apy / (double)minimum;
				var top = terms[^1].Apy / (double)veterans.MaxApy[position];

				if (Math.Abs(bottom - top) > 1e-6 || Math.Abs(bottom - 1) > veterans.Wiggle.Max + 1e-6)
				{
					offenders.Add($"{position}, seed {seed}: {bottom:F4} × the minimum at rank 0, {top:F4} × the max at rank 1");
				}
			}
		}

		offenders.ShouldBeEmpty();
	}

	[Fact]
	public void The_wiggle_is_clamped_at_its_max_both_ways()
	{
		// An sd of 10 puts nearly every draw past ±20%
		var wild = new ContractGenerator(Fixture with { Veterans = Fixture.Veterans with { Wiggle = new Wiggle(10, 0.20) } }, 32);
		var wiggles = Enumerable.Range(0, 200)
			.Select(s => wild.DrawVeteranTerms(new GfoRandom((ulong)s), Position.QB, 1.0, PlayerTier.Starter, 28, 6).Apy / 45_000_000.0 - 1)
			.ToList();

		wiggles.Min().ShouldBe(-0.20, tolerance: 1e-6);
		wiggles.Max().ShouldBe(0.20, tolerance: 1e-6);
	}

	[Theory]
	[InlineData(-0.01)]
	[InlineData(1.01)]
	[InlineData(double.NaN)]
	public void A_rank_outside_0_to_1_throws(double rank)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => Worked.DrawVeteranTerms(new GfoRandom(1), Position.QB, rank, PlayerTier.Starter, 28, 6));
	}

	// ---- Veteran deals ----

	[Theory]
	[InlineData(1_275_750, ContractType.Minimum)]
	[InlineData(1_275_751, ContractType.Veteran)]
	public void An_apy_within_5_percent_of_his_minimum_is_a_one_season_minimum_deal(long apy, ContractType expected)
	{
		// Experience 5: a $1,215,000 minimum, so the line is $1,275,750
		var contract = Worked.Veteran(new VeteranTerms(apy, 3, 1, 0.30, 0.50), 5, Season);

		contract.Type.ShouldBe(expected);

		if (expected == ContractType.Minimum)
		{
			contract.SignedSeason.ShouldBe(Season);
			contract.SigningBonus.ShouldBe(0);
			contract.GuaranteedTotal.ShouldBe(0);
			contract.Seasons.ShouldBe([new ContractSeason(Season, 1_215_000, 0, 0, 1_215_000)]);
		}
	}

	[Fact]
	public void A_veteran_deal_is_paid_as_F_12_works_it_out()
	{
		// $10M a year for 4 years, signed a season ago: a 30% bonus, the rest rising 8% a season, half of it guaranteed
		var contract = Worked.Veteran(new VeteranTerms(10_000_000, 4, 1, 0.30, 0.50), 6, Season);
		var bases = contract.Seasons.Select(s => s.BaseSalary).ToList();

		contract.Type.ShouldBe(ContractType.Veteran);
		contract.SignedSeason.ShouldBe(2025);
		contract.TotalValue.ShouldBe(40_000_000);
		contract.SigningBonus.ShouldBe(12_000_000);
		contract.GuaranteedTotal.ShouldBe(20_000_000);
		contract.Seasons.Select(s => s.Season).ShouldBe([2025, 2026, 2027, 2028]);
		contract.Seasons.ShouldAllBe(s => s.ProratedBonus == 3_000_000 && s.CapHit == s.BaseSalary + 3_000_000);

		// $28M of bases split 1 : 1.08 : 1.16 : 1.24 is 6.25M / 6.75M / 7.25M / 7.75M by hand. The weights sum to 4.48,
		// which has no exact binary form, so the floor can land a dollar under, and the last season takes the difference.
		bases.Zip([6_250_000L, 6_750_000, 7_250_000, 7_750_000]).ShouldAllBe(b => Math.Abs(b.First - b.Second) <= 2);
		bases.Sum().ShouldBe(28_000_000);

		// The guarantee: the $12M bonus, then $8M of bases from the first season on
		contract.Seasons.Select(s => s.GuaranteedAmount).ShouldBe([bases[0], 8_000_000 - bases[0], 0, 0]);
	}

	[Fact]
	public void A_base_under_its_minimum_is_raised_and_the_last_season_pays_it_back()
	{
		// $1,296,000 for 3 years, no bonus: the bases split 1,200,000 / 1,296,000 / 1,392,000. The first is under his
		// $1,215,000 minimum, so it rises by 15,000 and the last gives the 15,000 back: the total stays 3 × APY.
		var contract = Worked.Veteran(new VeteranTerms(1_296_000, 3, 2, 0, 0.20), 6, Season);

		contract.TotalValue.ShouldBe(3_888_000);
		contract.Seasons.Select(s => s.BaseSalary).ShouldBe([1_215_000, 1_296_000, 1_377_000]);
	}

	[Fact]
	public void When_the_last_season_cannot_pay_the_raises_back_the_total_grows()
	{
		// $1.9M for 3 years with a 40% bonus leaves 3,420,000 of bases: 1,055,555 / 1,140,000 / 1,224,445. The first two
		// rise to $1,215,000, and the last can give back only 9,445 before it reaches its own minimum (F-12 §10).
		var contract = Worked.Veteran(new VeteranTerms(1_900_000, 3, 1, 0.40, 0.55), 5, Season);

		contract.SigningBonus.ShouldBe(2_280_000);
		contract.Seasons.Select(s => s.BaseSalary).ShouldBe([1_215_000, 1_215_000, 1_215_000]);
		contract.TotalValue.ShouldBe(5_925_000);
		contract.GuaranteedTotal.ShouldBe(3_258_750);
	}

	[Fact]
	public void Each_season_s_floor_is_his_minimum_in_that_season()
	{
		// Signed two seasons ago with 5 credited seasons, so the three seasons are his 5th, 6th and 7th: the last one's
		// minimum is the $1,300,000 step, the first two $1,215,000
		var contract = Worked.Veteran(new VeteranTerms(1_400_000, 3, 2, 0.40, 0.50), 7, Season);

		contract.SignedSeason.ShouldBe(2024);
		contract.Seasons.Select(s => s.BaseSalary).ShouldBe([1_215_000, 1_215_000, 1_300_000]);
	}

	[Fact]
	public void Money_rounds_half_away_from_zero()
	{
		// 1,000,001 × 0.5 is 500,000.5 exactly
		Worked.Veteran(new VeteranTerms(1_000_001, 1, 0, 0.5, 0), 0, Season).SigningBonus.ShouldBe(500_001);
	}

	[Fact]
	public void Every_drawn_deal_keeps_the_veteran_and_minimum_rules()
	{
		var offenders = new List<string>();
		var (veterans, minimums, raised) = (0, 0, 0);
		var sample =
			from tier in ContractTiers
			from age in SampleAges
			from experience in SampleExperience
			from position in Enum.GetValues<Position>()
			from rank in SampleRanks
			from seed in Enumerable.Range(0, 10)
			select (tier, age, experience, position, rank, Seed: (ulong)seed);

		foreach (var (tier, age, experience, position, rank, seed) in sample)
		{
			var terms = Shipped.DrawVeteranTerms(new GfoRandom(seed), position, rank, tier, age, experience);
			var contract = Shipped.Veteran(terms, experience, Season);
			var label = $"{position} {tier} at {age}, experience {experience}, rank {rank}, seed {seed}";
			var broken = Broken(contract).ToList();

			if (terms.Apy <= 1.05 * Shipped.Minimum(experience))
			{
				minimums++;
				if (contract.Type != ContractType.Minimum || contract.SignedSeason != Season || contract.Seasons.Count != 1
					|| contract.Seasons[0].BaseSalary != Shipped.Minimum(experience) || contract.SigningBonus != 0 || contract.GuaranteedTotal != 0)
				{
					broken.Add($"not a one-season minimum deal: {contract.Type}, {contract.Seasons.Count} seasons");
				}
			}
			else
			{
				veterans++;
				var bases = contract.Seasons.Select(s => s.BaseSalary).ToList();
				var floors = bases.Select((_, i) => Shipped.Minimum(experience - terms.Elapsed + i)).ToList();

				if (contract.Type != ContractType.Veteran || contract.SignedSeason != Season - terms.Elapsed || bases.Count != terms.Years)
				{
					broken.Add($"{contract.Type}, signed {contract.SignedSeason}, {bases.Count} seasons");
				}

				if (bases.Where((b, i) => b < floors[i]).Any())
				{
					broken.Add("a base under his minimum that season");
				}

				if (contract.SigningBonus != Dollars(terms.Apy * terms.Years * terms.BonusShare))
				{
					broken.Add($"bonus {contract.SigningBonus:N0}");
				}

				if (contract.GuaranteedTotal != Guaranteed(contract, terms.GuaranteeShare))
				{
					broken.Add($"guaranteed {contract.GuaranteedTotal:N0}");
				}

				// The floors only ever add money; with no base at its floor nothing was raised, so the draw is paid exactly
				if (contract.TotalValue < terms.Apy * terms.Years)
				{
					broken.Add($"total {contract.TotalValue:N0} under {terms.Apy * terms.Years:N0}");
				}
				else if (bases.Where((b, i) => b == floors[i]).Any())
				{
					raised++;
				}
				else if (contract.TotalValue != terms.Apy * terms.Years || bases.Zip(bases.Skip(1)).Any(b => b.Second < b.First))
				{
					broken.Add("nothing raised, yet the total or the rising bases are off");
				}
			}

			offenders.AddRange(broken.Select(b => $"{label}: {b}"));
		}

		offenders.Distinct().Take(20).ShouldBeEmpty();

		// The sample must hold both kinds of deal and some raised floors, or the rules above checked nothing
		minimums.ShouldBeGreaterThan(0);
		veterans.ShouldBeGreaterThan(0);
		raised.ShouldBeGreaterThan(0);
	}

	// ---- Helpers ----

	private static DraftInfo Draft(int round, int pick) => new(2024, round, pick, Team);

	private static long Dollars(double amount) => (long)Math.Round(amount, MidpointRounding.AwayFromZero);

	private static bool Within(double value, Share share) => value >= share.Min && value <= share.Max;

	/// <summary>The guide's rule: the share of the total, but never less than the bonus or more than the whole deal.</summary>
	private static long Guaranteed(GeneratedContract contract, double share) =>
		Math.Clamp(Dollars(contract.TotalValue * share), contract.SigningBonus, contract.TotalValue);

	/// <summary>
	/// What every deal keeps (the guide's "every deal" row): seasons in a row from the signed one, the bases and the bonus
	/// adding up to the total, CapMath's proration and cap hits, and guarantees that fill each season before the next.
	/// </summary>
	private static IEnumerable<string> Broken(GeneratedContract contract)
	{
		var years = contract.Seasons.Count;

		if (!contract.Seasons.Select(s => s.Season).SequenceEqual(Enumerable.Range(contract.SignedSeason, years)))
		{
			yield return "seasons not in a row from the signed season";
		}

		if (contract.Seasons.Sum(s => s.BaseSalary) + contract.SigningBonus != contract.TotalValue)
		{
			yield return "bases and bonus don't add up to the total";
		}

		if (contract.SigningBonus + contract.Seasons.Sum(s => s.GuaranteedAmount) != contract.GuaranteedTotal)
		{
			yield return "the guarantees don't add up";
		}

		for (var i = 0; i < years; i++)
		{
			var season = contract.Seasons[i];

			if (season.ProratedBonus != CapMath.ProratedBonus(contract.SigningBonus, years, i)
				|| season.CapHit != CapMath.CapHit(season.BaseSalary, contract.SigningBonus, years, i))
			{
				yield return $"season {i}: proration or cap hit isn't CapMath's";
			}

			if (season.GuaranteedAmount < 0 || season.GuaranteedAmount > season.BaseSalary)
			{
				yield return $"season {i}: guaranteed {season.GuaranteedAmount:N0} of a {season.BaseSalary:N0} base";
			}

			if (i > 0 && season.GuaranteedAmount > 0 && contract.Seasons[i - 1].GuaranteedAmount < contract.Seasons[i - 1].BaseSalary)
			{
				yield return $"season {i}: guaranteed before season {i - 1} is full";
			}
		}
	}
}
