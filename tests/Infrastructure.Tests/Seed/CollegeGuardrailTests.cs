using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// D-035 against the shipped college list: fictional names only, no duplicates, and every region holding every tier
/// in about the planned mix (WP2 guide §2.2). Each test lists every offender.
/// </summary>
public sealed class CollegeGuardrailTests
{
    private const int TierTolerance = 10;

    private static readonly IReadOnlyList<CollegeDefinition> Colleges =
        new EmbeddedSeedData(NullLogger<EmbeddedSeedData>.Instance).Colleges.Colleges;

    [Fact]
    public void College_names_are_unique()
    {
        Colleges.GroupBy(c => RepoFiles.NameKey(c.Name))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.First().Name} appears {g.Count()} times")
            .ShouldBeEmpty();
    }

    [Fact]
    public void No_college_is_a_real_institution()
    {
        var real = RepoFiles.Names("real-colleges.json");

        Colleges.Where(c => real.Contains(RepoFiles.NameKey(c.Name)))
            .Select(c => c.Name)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Every_region_has_every_tier_in_about_the_planned_mix()
    {
        var planned = new Dictionary<CollegeTier, int> { [CollegeTier.Power] = 50, [CollegeTier.Major] = 80, [CollegeTier.Small] = 100 };

        var missing =
            from region in Enum.GetValues<CollegeRegion>()
            from tier in Enum.GetValues<CollegeTier>()
            where !Colleges.Any(c => c.Region == region && c.Tier == tier)
            select $"{region} has no {tier} college";
        var offMix =
            from tier in Enum.GetValues<CollegeTier>()
            let count = Colleges.Count(c => c.Tier == tier)
            where Math.Abs(count - planned[tier]) > TierTolerance
            select $"{count} {tier} colleges, planned {planned[tier]} ± {TierTolerance}";

        missing.Concat(offMix).ShouldBeEmpty();
    }
}
