using System.Text.RegularExpressions;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// The shipped name and place lists (M1 WP3 guide §2.4, §4): large enough for a league's worth of players, every
/// entry usable, nothing repeated. Each rule lists every offender.
/// </summary>
public sealed partial class NameDataTests
{
    private static readonly EmbeddedSeedData Seed = new(NullLogger<EmbeddedSeedData>.Instance);

    public static TheoryData<string, int> Lists => new()
    {
        { "first", 1000 },
        { "last", 5000 },
    };

    [Theory]
    [MemberData(nameof(Lists))]
    public void A_name_list_is_large_enough(string list, int atLeast) =>
        Names(list).Names.Count.ShouldBeGreaterThanOrEqualTo(atLeast, $"The {list} names are too few for a league.");

    [Theory]
    [InlineData("first")]
    [InlineData("last")]
    public void No_name_is_empty(string list)
    {
        Names(list).Names
            .Select((entry, index) => (entry, index))
            .Where(n => string.IsNullOrWhiteSpace(n.entry.Name))
            .Select(n => $"{list} names: entry {n.index} is empty")
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData("first")]
    [InlineData("last")]
    public void No_name_repeats(string list)
    {
        Names(list).Names
            .GroupBy(n => RepoFiles.NameKey(n.Name))
            .Where(g => g.Count() > 1)
            .Select(g => $"{list} names: {g.First().Name} appears {g.Count()} times")
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData("first")]
    [InlineData("last")]
    public void Every_name_has_a_positive_weight(string list)
    {
        Names(list).Names
            .Where(n => n.Weight <= 0)
            .Select(n => $"{list} names: {n.Name} has weight {n.Weight}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void The_place_list_is_large_enough() =>
        Seed.Hometowns.Places.Count.ShouldBeGreaterThanOrEqualTo(1000, "The hometowns are too few for a league.");

    [Fact]
    public void No_place_is_empty_or_repeated_within_its_state()
    {
        var places = Seed.Hometowns.Places;
        var offenders = places
            .Select((place, index) => (place, index))
            .Where(p => string.IsNullOrWhiteSpace(p.place.Name))
            .Select(p => $"entry {p.index} has no name")
            .ToList();

        offenders.AddRange(places
            .GroupBy(p => (RepoFiles.NameKey(p.Name), p.State))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.First().Name}, {g.Key.State} appears {g.Count()} times"));

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Every_place_has_a_population_and_a_two_letter_state()
    {
        Seed.Hometowns.Places
            .Where(p => p.Population <= 0 || !StateCode().IsMatch(p.State))
            .Select(p => $"{p.Name}, {p.State}: population {p.Population}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void Every_list_names_its_source()
    {
        new[] { ("first", Seed.FirstNames.Source), ("last", Seed.LastNames.Source), ("hometowns", Seed.Hometowns.Source) }
            .Where(l => string.IsNullOrWhiteSpace(l.Item2))
            .Select(l => $"{l.Item1} has no source")
            .ShouldBeEmpty();
    }

    private static NameList Names(string list) => list == "first" ? Seed.FirstNames : Seed.LastNames;

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex StateCode();
}
