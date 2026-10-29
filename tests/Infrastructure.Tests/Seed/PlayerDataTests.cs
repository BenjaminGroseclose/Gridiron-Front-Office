using System.Globalization;
using System.Text.Json;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// The shipped player data files (M1 WP3 guide §4) through the embedded loader: every file parses with the strict
/// options, and the numbers are consistent with each other and with F-03. Each rule lists every offender, so a data
/// change that breaks one says exactly where.
/// </summary>
public sealed class PlayerDataTests
{
    private const double Tolerance = 1e-6;
    private const int ActiveRoster = 53;

    private static readonly EmbeddedSeedData Seed = new(NullLogger<EmbeddedSeedData>.Instance);
    private static readonly IReadOnlyList<PositionProfile> Positions = Seed.Positions.Positions;
    private static readonly GenerationSettings Generation = Seed.Generation;

    /// <summary>The ratings F-03 §4.3 derives from measurables.</summary>
    private static readonly PlayerAttribute[] PhysicalRatings =
        [PlayerAttribute.Speed, PlayerAttribute.Acceleration, PlayerAttribute.Agility, PlayerAttribute.Strength];

    /// <summary>Names the blocklist must hold (F-03 §4.11).</summary>
    private static readonly string[] KnownFamousPeople = ["Jerry Rice", "Tom Brady"];

    // ---- Every file loads ----

    [Fact]
    public void Every_position_file_loads()
    {
        Positions.Count.ShouldBe(Enum.GetValues<Position>().Length);
        Positions.ShouldAllBe(p => p.Measurables.Count == Enum.GetValues<Measurable>().Length);
        Positions.ShouldAllBe(p => p.Archetypes.Count > 0 && p.OvrWeights.Count > 0);
    }

    [Fact]
    public void The_generation_settings_load()
    {
        Generation.Roster.Active.ShouldBe(ActiveRoster);
        Generation.Tiers.Keys.ShouldBe(Enum.GetValues<PlayerTier>(), ignoreOrder: true);
        Generation.Athletic.Ratings.Count.ShouldBeGreaterThan(0);
        Generation.Hidden.PotentialGrades.Count.ShouldBe(Enum.GetValues<PotentialGrade>().Length);
        Generation.Hidden.DevelopmentCurve.Keys.ShouldBe(Enum.GetValues<DevelopmentCurve>(), ignoreOrder: true);
        Generation.CollegeTiers.Keys.ShouldBe(Enum.GetValues<PlayerTier>(), ignoreOrder: true);
    }

    [Fact]
    public void The_jersey_rules_load() =>
        Seed.JerseyRules.Rules.Count.ShouldBe(Enum.GetValues<Position>().Length);

    [Fact]
    public void A_distribution_keeps_its_optional_bounds_only_when_the_file_gives_them()
    {
        var age = Positions[0].Age;
        var height = Positions[0].Measurables[Measurable.Height];

        (age.Min, age.Max).ShouldNotBe((null, null));
        (height.Min, height.Max).ShouldBe((null, null));
    }

    // ---- Every position is described once ----

    [Fact]
    public void Every_position_is_described_exactly_once()
    {
        var counts = Positions.GroupBy(p => p.Position).ToDictionary(g => g.Key, g => g.Count());

        Enum.GetValues<Position>()
            .Where(position => counts.GetValueOrDefault(position) != 1)
            .Select(position => $"{position} is described {counts.GetValueOrDefault(position)} times")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Every_position_has_all_eleven_measurables_with_a_spread()
    {
        var offenders =
            from profile in Positions
            from measurable in Enum.GetValues<Measurable>()
            let distribution = profile.Measurables.GetValueOrDefault(measurable)
            where distribution is null || distribution.Sd <= 0
            select $"{profile.Position} {measurable}: {(distribution is null ? "missing" : $"sd {distribution.Sd}")}";

        offenders.ShouldBeEmpty();
    }

    // ---- Roster targets add up ----

    [Fact]
    public void Roster_averages_sum_to_the_active_roster()
    {
        var total = Positions.Sum(p => p.Roster.Average);

        Math.Abs(total - ActiveRoster).ShouldBeLessThan(Tolerance, $"The averages sum to {total}, not {ActiveRoster}.");
    }

    [Fact]
    public void Each_roster_target_is_ordered_floor_min_average_max_with_starters_within_the_floor()
    {
        Positions
            .Where(p => !(p.Roster.Floor <= p.Roster.Min && p.Roster.Min <= p.Roster.Average
                          && p.Roster.Average <= p.Roster.Max && p.Roster.Starters <= p.Roster.Floor))
            .Select(p => $"{p.Position}: floor {p.Roster.Floor}, min {p.Roster.Min}, average {p.Roster.Average}, max {p.Roster.Max}, starters {p.Roster.Starters}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void The_floors_fit_in_the_roster_and_the_maxima_reach_it()
    {
        var floors = Positions.Sum(p => p.Roster.Floor);
        var maxima = Positions.Sum(p => p.Roster.Max);

        floors.ShouldBeLessThanOrEqualTo(ActiveRoster, $"The floors need {floors} players.");
        maxima.ShouldBeGreaterThanOrEqualTo(ActiveRoster, $"The maxima only allow {maxima} players.");
    }

    // ---- Ages are possible ----

    [Fact]
    public void Ages_are_possible()
    {
        Positions
            .Where(p => !(p.Age.Min is >= 21 && p.Age.Min < p.Age.Mean && p.Age.Mean < p.Age.Max))
            .Select(p => $"{p.Position}: min {p.Age.Min}, mean {p.Age.Mean}, max {p.Age.Max}")
            .ShouldBeEmpty();
    }

    // ---- OVR weights sum to 1 ----

    [Fact]
    public void Ovr_weights_sum_to_one_per_position()
    {
        Positions
            .Select(p => (p.Position, Sum: p.OvrWeights.Values.Sum()))
            .Where(p => Math.Abs(p.Sum - 1) > Tolerance)
            .Select(p => $"{p.Position}: weights sum to {p.Sum.ToString("0.####", CultureInfo.InvariantCulture)}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Every_ovr_weight_is_positive()
    {
        var offenders =
            from profile in Positions
            from weight in profile.OvrWeights
            where weight.Value <= 0
            select $"{profile.Position} {weight.Key}: {weight.Value}";

        offenders.ShouldBeEmpty();
    }

    // ---- Archetypes are well formed ----

    [Fact]
    public void Every_position_has_archetypes_whose_shares_sum_to_one()
    {
        Positions
            .Select(p => (p.Position, Count: p.Archetypes.Count, Sum: p.Archetypes.Sum(a => a.Share)))
            .Where(p => p.Count == 0 || Math.Abs(p.Sum - 1) > Tolerance)
            .Select(p => $"{p.Position}: {p.Count} archetypes, shares sum to {p.Sum.ToString("0.####", CultureInfo.InvariantCulture)}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Archetype_names_are_unique_within_a_position()
    {
        var offenders =
            from profile in Positions
            from sameName in profile.Archetypes.GroupBy(a => RepoFiles.NameKey(a.Name))
            where sameName.Count() > 1
            select $"{profile.Position}: {sameName.First().Name} appears {sameName.Count()} times";

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Archetype_skill_means_are_ratings()
    {
        var offenders =
            from profile in Positions
            from archetype in profile.Archetypes
            from skill in archetype.Skills
            where skill.Value is < 0 or > 100
            select $"{profile.Position} {archetype.Name} {skill.Key}: {skill.Value}";

        offenders.ShouldBeEmpty();
    }

    // ---- Every attribute is used (D-050) ----

    [Fact]
    public void Every_attribute_is_read_by_some_part_of_generation()
    {
        var used = new HashSet<PlayerAttribute>(Generation.Skills.Common.Keys);
        used.UnionWith(Generation.Athletic.Ratings.Select(r => r.Attribute));
        used.UnionWith(Positions.SelectMany(p => p.OvrWeights.Keys));
        used.UnionWith(Positions.SelectMany(p => p.Archetypes).SelectMany(a => a.Skills.Keys));

        Enum.GetValues<PlayerAttribute>()
            .Where(attribute => !used.Contains(attribute))
            .Select(attribute => $"{attribute} is in no OVR weights, archetype, common skill or physical rating map")
            .ShouldBeEmpty();
    }

    // ---- Generation settings are complete ----

    [Fact]
    public void Every_tier_has_settings()
    {
        Enum.GetValues<PlayerTier>()
            .Where(tier => !Generation.Tiers.ContainsKey(tier))
            .Select(tier => $"{tier} has no tier settings")
            .ShouldBeEmpty();
    }

    [Fact]
    public void The_four_physical_ratings_each_have_a_map_from_a_measurable()
    {
        var mapped = Generation.Athletic.Ratings.Select(r => r.Attribute).ToHashSet();

        PhysicalRatings.Where(attribute => !mapped.Contains(attribute))
            .Select(attribute => $"{attribute} has no measurable mapped to it")
            .ShouldBeEmpty();

        Generation.Athletic.Ratings
            .Where(r => r.Best.Value == r.Worst.Value || r.Best.Rating == r.Worst.Rating)
            .Select(r => $"{r.Measurable} → {r.Attribute}: best {r.Best} and worst {r.Worst} don't differ")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Potential_grades_cover_every_grade_in_descending_order_down_to_zero()
    {
        var bands = Generation.Hidden.PotentialGrades;
        var offenders = new List<string>();

        var counts = bands.GroupBy(b => b.Grade).ToDictionary(g => g.Key, g => g.Count());
        offenders.AddRange(Enum.GetValues<PotentialGrade>()
            .Where(grade => counts.GetValueOrDefault(grade) != 1)
            .Select(grade => $"{grade} appears {counts.GetValueOrDefault(grade)} times"));

        offenders.AddRange(bands.Zip(bands.Skip(1))
            .Where(pair => pair.First.MinPeakOvr <= pair.Second.MinPeakOvr || pair.First.Grade <= pair.Second.Grade)
            .Select(pair => $"{pair.First.Grade} ({pair.First.MinPeakOvr}) is followed by {pair.Second.Grade} ({pair.Second.MinPeakOvr})"));

        if (bands.Count > 0 && bands[^1].MinPeakOvr != 0)
        {
            offenders.Add($"The last band ({bands[^1].Grade}) starts at {bands[^1].MinPeakOvr}, not 0");
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Development_curve_shares_sum_to_one()
    {
        var sum = Generation.Hidden.DevelopmentCurve.Values.Sum();

        Math.Abs(sum - 1).ShouldBeLessThan(Tolerance, $"The development curve shares sum to {sum}.");
    }

    [Fact]
    public void College_tier_shares_sum_to_one_for_every_player_tier()
    {
        var offenders = new List<string>();
        foreach (var tier in Enum.GetValues<PlayerTier>())
        {
            if (!Generation.CollegeTiers.TryGetValue(tier, out var shares))
            {
                offenders.Add($"{tier} has no college tier shares");
                continue;
            }

            offenders.AddRange(Enum.GetValues<CollegeTier>()
                .Where(college => !shares.ContainsKey(college))
                .Select(college => $"{tier} has no share for {college} colleges"));

            var sum = shares.Values.Sum();
            if (Math.Abs(sum - 1) > Tolerance)
            {
                offenders.Add($"{tier}: college tier shares sum to {sum.ToString("0.####", CultureInfo.InvariantCulture)}");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Potential_growth_ages_ascend_and_reach_99()
    {
        var bands = Generation.Hidden.PotentialGrowth;
        var offenders = bands.Zip(bands.Skip(1))
            .Where(pair => pair.First.ThroughAge >= pair.Second.ThroughAge)
            .Select(pair => $"through {pair.First.ThroughAge} is followed by through {pair.Second.ThroughAge}")
            .ToList();

        if (bands.Count == 0 || bands[^1].ThroughAge != 99)
        {
            offenders.Add($"The last band stops at {(bands.Count == 0 ? "nothing" : bands[^1].ThroughAge)}, not 99");
        }

        offenders.ShouldBeEmpty();
    }

    // ---- WP4's generation rules (M1 WP4 guide §5, slice 1) ----

    [Fact]
    public void Roster_averages_are_whole_numbers()
    {
        // The mirrored roster mix and the free-agent split both start from whole counts.
        Positions
            .Where(p => p.Roster.Average != Math.Floor(p.Roster.Average))
            .Select(p => $"{p.Position}: average {p.Roster.Average}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void No_archetype_lists_a_physical_rating()
    {
        // Speed, Acceleration, Agility and Strength come only from the measurables (F-03 §4.3).
        var offenders =
            from profile in Positions
            from archetype in profile.Archetypes
            from attribute in PhysicalRatings
            where archetype.Skills.ContainsKey(attribute)
            select $"{profile.Position} {archetype.Name} lists {attribute}";

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void The_draft_settings_are_possible()
    {
        var draft = Generation.Draft;
        var offenders = new List<string>();

        if (draft.DraftedShare is <= 0 or > 1)
        {
            offenders.Add($"draftedShare {draft.DraftedShare} isn't within (0, 1]");
        }

        if (draft.RankNoiseSd < 0)
        {
            offenders.Add($"rankNoiseSd {draft.RankNoiseSd} is negative");
        }

        if (draft.EntryAge.Min > draft.EntryAge.Max)
        {
            offenders.Add($"entryAge {draft.EntryAge.Min}–{draft.EntryAge.Max} is backwards");
        }

        // A player can't enter the league older than the youngest age his position allows.
        offenders.AddRange(Positions
            .Where(p => draft.EntryAge.Min > p.Age.Min)
            .Select(p => $"{p.Position}: age min {p.Age.Min} is under entryAge.min {draft.EntryAge.Min}"));

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void The_roster_mix_and_the_strength_ladder_are_not_negative()
    {
        var strength = Generation.TeamStrength;

        Generation.Roster.MixSwaps.ShouldBeGreaterThanOrEqualTo(0);
        new[] { ("range", strength.Range), ("youngWeight", strength.YoungWeight), ("veteranWeight", strength.VeteranWeight) }
            .Where(setting => setting.Item2 < 0)
            .Select(setting => $"teamStrength.{setting.Item1} is {setting.Item2}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Every_archetype_at_a_position_has_the_same_expected_starter_ovr()
    {
        // D-084: style alone never costs OVR, pay or draft position. The physical ratings are the same for every
        // archetype, so they're left out of the comparison.
        var shift = Generation.Tiers[PlayerTier.Starter].SkillShift;
        var skills = Generation.Skills;

        var offenders = new List<string>();
        foreach (var profile in Positions.Where(p => p.Archetypes.Count > 1))
        {
            var expected = profile.Archetypes.ToDictionary(a => a.Name, a => profile.OvrWeights
                .Where(w => !PhysicalRatings.Contains(w.Key))
                .Sum(w => w.Value * (a.Skills.TryGetValue(w.Key, out var mean) ? mean + shift
                    : skills.Common.TryGetValue(w.Key, out var common) ? common + shift
                    : skills.OffPosition.Mean)));

            var gap = expected.Values.Max() - expected.Values.Min();
            if (gap > 1)
            {
                offenders.Add($"{profile.Position}: {string.Join(", ", expected.Select(e => $"{e.Key} {e.Value.ToString("0.00", CultureInfo.InvariantCulture)}"))}");
            }
        }

        offenders.ShouldBeEmpty();
    }

    // ---- The specialists (D-088) ----

    [Fact]
    public void Spread_scales_are_within_0_to_1()
    {
        // The scale only narrows: it's under 1 where OVR is one or two skills, which the quality draw would spread wider
        // than the tier's target
        Positions
            .Where(p => p.SpreadScale is <= 0 or > 1)
            .Select(p => $"{p.Position}: spreadScale {p.SpreadScale.ToString(CultureInfo.InvariantCulture)}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Earliest_rounds_are_draft_rounds_and_some_position_can_go_first()
    {
        var rounds = Seed.Contracts.RookieScale.Rounds.Count;

        Positions
            .Where(p => p.EarliestRound < 1 || p.EarliestRound > rounds)
            .Select(p => $"{p.Position}: earliestRound {p.EarliestRound} isn't a round from 1 to {rounds}")
            .ShouldBeEmpty();
        Positions.ShouldContain(p => p.EarliestRound == 1, "no position can go in round 1");
    }

    [Fact]
    public void Draft_penalties_are_zero_or_more_and_only_for_positions_held_back()
    {
        // The penalty spreads a held-back position over the rounds after its earliest one. One on a position that may go
        // in round 1 would be a value per position, which is the draft AI's (IDEAS.md)
        Positions
            .Where(p => p.DraftPenalty < 0 || (p.DraftPenalty > 0 && p.EarliestRound == 1))
            .Select(p => $"{p.Position}: draftPenalty {p.DraftPenalty.ToString(CultureInfo.InvariantCulture)} with earliestRound {p.EarliestRound}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void A_position_held_back_in_the_draft_never_earns_more_as_a_rookie_than_its_best_veteran()
    {
        // Why the specialists wait for round 4: a round-3 deal (about $1.6M a year) pays a long snapper more than his
        // position's $1.5M veteran max. The best deal of a position's earliest round is that round's first pick
        var contracts = Seed.Contracts;
        var offenders =
            from profile in Positions
            where profile.EarliestRound > 1
            let apy = contracts.RookieScale.Rounds[profile.EarliestRound - 1].FirstPick / contracts.RookieScale.Years
            let max = contracts.Veterans.MaxApy[profile.Position]
            where apy > max
            select $"{profile.Position}: round {profile.EarliestRound}'s first pick pays {apy:N0} a year, over the {max:N0} max";

        offenders.ShouldBeEmpty();
    }

    // ---- Jersey numbers exist ----

    [Fact]
    public void Every_position_has_one_jersey_rule_with_numbers_from_0_to_99()
    {
        var rules = Seed.JerseyRules.Rules;
        var offenders = new List<string>();

        var counts = rules.GroupBy(r => r.Position).ToDictionary(g => g.Key, g => g.Count());
        offenders.AddRange(Enum.GetValues<Position>()
            .Where(position => counts.GetValueOrDefault(position) != 1)
            .Select(position => $"{position} has {counts.GetValueOrDefault(position)} rules"));

        offenders.AddRange(
            from rule in rules
            from range in rule.Numbers
            where range.From < 0 || range.To > 99 || range.From > range.To
            select $"{rule.Position}: {range.From}–{range.To} isn't a range within 0–99");

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Every_position_has_at_least_as_many_numbers_as_its_roster_maximum()
    {
        var rules = Seed.JerseyRules.Rules.ToDictionary(r => r.Position, r => r.Numbers);

        Positions
            .Select(p => (p.Position, Max: p.Roster.Max, Available: Available(rules.GetValueOrDefault(p.Position))))
            .Where(p => p.Available < p.Max)
            .Select(p => $"{p.Position}: {p.Available} numbers for up to {p.Max} players")
            .ShouldBeEmpty();
    }

    // ---- Famous people are blocked ----

    [Fact]
    public void Famous_people_are_blocked()
    {
        var keys = Seed.FamousPeople.Select(RepoFiles.NameKey).ToHashSet();

        Seed.FamousPeople.ShouldNotBeEmpty();
        KnownFamousPeople
            .Where(name => !keys.Contains(RepoFiles.NameKey(name)))
            .Select(name => $"{name} isn't in the blocklist")
            .ShouldBeEmpty();
    }

    // ---- A misspelled enum key fails ----

    [Fact]
    public void A_misspelled_enum_key_fails_to_parse()
    {
        const string misspelled = """{ "name": "Man", "share": 1, "skills": { "RoutRunning": 70 } }""";
        const string spelled = """{ "name": "Man", "share": 1, "skills": { "RouteRunning": 70 } }""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Archetype>(misspelled, EmbeddedSeedData.JsonOptions));
        JsonSerializer.Deserialize<Archetype>(spelled, EmbeddedSeedData.JsonOptions)!.Skills.Keys.ShouldBe([PlayerAttribute.RouteRunning]);
    }

    private static int Available(IReadOnlyList<NumberRange>? ranges) =>
        ranges is null ? 0 : ranges.SelectMany(r => Enumerable.Range(r.From, r.To - r.From + 1)).Distinct().Count();
}
