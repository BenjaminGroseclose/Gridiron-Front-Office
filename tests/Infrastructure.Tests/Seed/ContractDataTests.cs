using System.Globalization;
using System.Text.Json;
using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// The shipped contract data (F-12 §4.10, rules R1–R10) through the embedded loader. The numbers must be consistent
/// with each other, with CapMath and with positions.json, so WP4's generator can trust them. Each rule lists every
/// offender.
/// </summary>
public sealed class ContractDataTests
{
    private const double Tolerance = 1e-6;

    private static readonly EmbeddedSeedData Seed = new(NullLogger<EmbeddedSeedData>.Instance);
    private static readonly ContractData Contracts = Seed.Contracts;
    private static readonly VeteranDeals Veterans = Contracts.Veterans;
    private static readonly IReadOnlyList<PositionProfile> Positions = Seed.Positions.Positions;

    /// <summary>The contract tiers that carry length and share bands (Fringe players have no contract).</summary>
    private static readonly PlayerTier[] ContractTiers = [PlayerTier.Starter, PlayerTier.Backup];

    // ---- R1 ----

    [Fact]
    public void The_file_loads_with_a_cap_and_a_utilization_band()
    {
        Contracts.Cap.ShouldBeGreaterThan(0);
        Contracts.Utilization.Min.ShouldBeGreaterThan(0);
        Contracts.Utilization.Min.ShouldBeLessThan(Contracts.Utilization.Max);
        Contracts.Utilization.Max.ShouldBeLessThanOrEqualTo(1);
    }

    // ---- R2 ----

    [Fact]
    public void Minimum_salaries_start_at_zero_seasons_and_ascend()
    {
        var steps = Contracts.MinimumSalaries;
        var offenders = new List<string>();

        if (steps.Count == 0 || steps[0].CreditedSeasons != 0)
        {
            offenders.Add("The ladder doesn't start at 0 credited seasons");
        }

        offenders.AddRange(steps.Where(s => s.Salary <= 0).Select(s => $"{s.CreditedSeasons} seasons: {s.Salary}"));
        offenders.AddRange(steps.Zip(steps.Skip(1))
            .Where(pair => pair.First.CreditedSeasons >= pair.Second.CreditedSeasons || pair.First.Salary >= pair.Second.Salary)
            .Select(pair => $"{pair.First.CreditedSeasons} seasons ({pair.First.Salary}) is followed by {pair.Second.CreditedSeasons} seasons ({pair.Second.Salary})"));

        offenders.ShouldBeEmpty();
    }

    // ---- R3 ----

    [Fact]
    public void The_rookie_scale_is_well_formed()
    {
        var scale = Contracts.RookieScale;
        var offenders = new List<string>();

        if (scale.Years is < 1 or > CapMath.MaxContractYears)
        {
            offenders.Add($"{scale.Years} years isn't within 1–{CapMath.MaxContractYears}");
        }

        if (scale.BaseWeights.Count != scale.Years || Math.Abs(scale.BaseWeights.Sum() - 1) > Tolerance)
        {
            offenders.Add($"baseWeights has {scale.BaseWeights.Count} entries summing to {scale.BaseWeights.Sum().ToString("0.####", CultureInfo.InvariantCulture)}");
        }

        var counts = scale.Rounds.GroupBy(r => r.Round).ToDictionary(g => g.Key, g => g.Count());
        offenders.AddRange(Enumerable.Range(1, 7)
            .Where(round => counts.GetValueOrDefault(round) != 1)
            .Select(round => $"round {round} appears {counts.GetValueOrDefault(round)} times"));

        offenders.AddRange(scale.Rounds
            .Where(r => r.FirstPick < r.LastPick || r.BonusShare is < 0 or > 1 || r.GuaranteeShare is < 0 or > 1)
            .Select(r => $"round {r.Round}: first {r.FirstPick}, last {r.LastPick}, bonus {r.BonusShare}, guarantee {r.GuaranteeShare}"));

        var ordered = scale.Rounds.OrderBy(r => r.Round).ToList();
        offenders.AddRange(ordered.Zip(ordered.Skip(1))
            .Where(pair => pair.First.LastPick < pair.Second.FirstPick
                           || pair.First.BonusShare < pair.Second.BonusShare
                           || pair.First.GuaranteeShare < pair.Second.GuaranteeShare)
            .Select(pair => $"round {pair.First.Round} ({pair.First.LastPick}, shares {pair.First.BonusShare}/{pair.First.GuaranteeShare}) is followed by round {pair.Second.Round} ({pair.Second.FirstPick}, shares {pair.Second.BonusShare}/{pair.Second.GuaranteeShare})"));

        offenders.ShouldBeEmpty();
    }

    // ---- R4 ----

    [Fact]
    public void Every_rookie_pick_covers_the_minimum_salaries_after_its_bonus()
    {
        var scale = Contracts.RookieScale;
        var minimums = Enumerable.Range(0, scale.Years).Sum(MinimumFor);

        scale.Rounds
            .Select(r => (r.Round, Bases: r.LastPick * (1 - r.BonusShare)))
            .Where(r => r.Bases < minimums)
            .Select(r => $"round {r.Round}: the last pick's bases come to {r.Bases:0} against {minimums} of minimums")
            .ShouldBeEmpty();
    }

    // ---- R5 ----

    [Fact]
    public void The_undrafted_deal_is_possible()
    {
        Contracts.Undrafted.Years.ShouldBeInRange(1, CapMath.MaxContractYears);
        Contracts.Undrafted.Bonus.ShouldBeGreaterThanOrEqualTo(0);
    }

    // ---- R6 ----

    [Fact]
    public void Every_position_has_a_maximum_above_the_top_minimum()
    {
        var topMinimum = Contracts.MinimumSalaries[^1].Salary;
        var offenders = Enum.GetValues<Position>()
            .Where(position => !Veterans.MaxApy.TryGetValue(position, out var max) || max < topMinimum)
            .Select(position => $"{position}: {(Veterans.MaxApy.TryGetValue(position, out var max) ? $"max {max}" : "no maximum")}")
            .ToList();

        if (Veterans.Curve < 1)
        {
            offenders.Add($"curve {Veterans.Curve} is below 1");
        }

        if (!(Veterans.Wiggle.Sd >= 0 && Veterans.Wiggle.Sd <= Veterans.Wiggle.Max && Veterans.Wiggle.Max <= 0.5))
        {
            offenders.Add($"wiggle sd {Veterans.Wiggle.Sd}, max {Veterans.Wiggle.Max}");
        }

        offenders.ShouldBeEmpty();
    }

    // ---- R7 ----

    [Fact]
    public void Starters_and_backups_have_length_bands_ascending_to_99()
    {
        var offenders = new List<string>();
        foreach (var tier in ContractTiers)
        {
            if (!Veterans.Lengths.TryGetValue(tier, out var bands) || bands.Count == 0)
            {
                offenders.Add($"{tier} has no length bands");
                continue;
            }

            offenders.AddRange(bands
                .Where(b => b.Min < 1 || b.Min > b.Max || b.Max > CapMath.MaxContractYears)
                .Select(b => $"{tier} through {b.ThroughAge}: {b.Min}–{b.Max} years"));
            offenders.AddRange(bands.Zip(bands.Skip(1))
                .Where(pair => pair.First.ThroughAge >= pair.Second.ThroughAge)
                .Select(pair => $"{tier}: through {pair.First.ThroughAge} is followed by through {pair.Second.ThroughAge}"));
            if (bands[^1].ThroughAge != 99)
            {
                offenders.Add($"{tier}: the last band stops at {bands[^1].ThroughAge}, not 99");
            }
        }

        offenders.ShouldBeEmpty();
    }

    // ---- R8 ----

    [Fact]
    public void Bonus_and_guarantee_shares_are_bands_within_zero_and_one()
    {
        var offenders = new List<string>();
        foreach (var (name, shares) in new[] { ("bonusShare", Veterans.BonusShare), ("guaranteeShare", Veterans.GuaranteeShare) })
        {
            foreach (var tier in ContractTiers)
            {
                if (!shares.TryGetValue(tier, out var share))
                {
                    offenders.Add($"{name} has no {tier} band");
                }
                else if (!(share.Min >= 0 && share.Min <= share.Max && share.Max <= 1))
                {
                    offenders.Add($"{name} {tier}: {share.Min}–{share.Max}");
                }
            }
        }

        if (Veterans.BaseEscalation < 0)
        {
            offenders.Add($"baseEscalation {Veterans.BaseEscalation} is negative");
        }

        offenders.ShouldBeEmpty();
    }

    // ---- R9 ----

    [Fact]
    public void The_curve_pays_a_starter_well_above_the_minimum_where_a_position_has_backups()
    {
        var minimum = MinimumFor(3);   // a typical veteran's minimum

        Positions
            .Where(p => p.Roster.Average > p.Roster.Starters)
            .Select(p => (p.Position, Apy: Apy(p.Position, 1 - (p.Roster.Starters / p.Roster.Average), minimum)))
            .Where(p => p.Apy < 1.5 * minimum)
            .Select(p => $"{p.Position}: the lowest Starter would earn {p.Apy:0}, under 1.5 × {minimum}")
            .ShouldBeEmpty();
    }

    // ---- R10 ----

    [Fact]
    public void The_maxima_price_a_roster_near_the_cap()
    {
        var minimum = MinimumFor(3);
        var expectedShare = 1 / (Veterans.Curve + 1);   // the mean of t^curve over a uniform percentile
        var cost = Positions.Where(p => Veterans.MaxApy.ContainsKey(p.Position))   // a missing position is R6's report, not this one's
            .Sum(p => p.Roster.Average * (minimum + ((Veterans.MaxApy[p.Position] - minimum) * expectedShare)));
        var ratio = cost / Contracts.Cap;

        ratio.ShouldBeInRange(0.8, 1.3, $"A roster at the curve's expected values costs {cost:0} = {ratio:0.00} × cap.");
    }

    // ---- A misspelled enum key fails ----

    [Fact]
    public void A_misspelled_enum_key_fails_to_parse()
    {
        const string misspelled = """{ "QB": 1, "EDG": 2 }""";
        const string spelled = """{ "QB": 1, "EDGE": 2 }""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Dictionary<Position, long>>(misspelled, EmbeddedSeedData.JsonOptions));
        JsonSerializer.Deserialize<Dictionary<Position, long>>(spelled, EmbeddedSeedData.JsonOptions)!.Keys.ShouldBe([Position.QB, Position.EDGE]);
    }

    /// <summary>The minimum salary for a player with this many credited seasons: the last ladder step at or below it.</summary>
    private static long MinimumFor(int creditedSeasons) =>
        Contracts.MinimumSalaries.Where(s => s.CreditedSeasons <= creditedSeasons).Select(s => s.Salary).DefaultIfEmpty(0).Last();

    /// <summary>F-12 §4.7's curve: <c>min + (max − min) × t^curve</c>, before the wiggle.</summary>
    private static double Apy(Position position, double t, long minimum) =>
        minimum + ((Veterans.MaxApy[position] - minimum) * Math.Pow(t, Veterans.Curve));
}
