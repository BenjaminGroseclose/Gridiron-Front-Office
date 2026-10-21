using System.Text.Json;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Infrastructure.Seed;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>The built-in league data loads, and the parser rejects what a typo would produce (D-035, D-077).</summary>
public sealed class EmbeddedSeedDataTests
{
    [Fact]
    public void The_shipped_team_set_and_college_list_load()
    {
        var data = new EmbeddedSeedData(NullLogger<EmbeddedSeedData>.Instance);

        data.TeamSet.Name.ShouldBe("Default");
        data.TeamSet.Conferences.Count.ShouldBe(2);
        data.TeamSet.Teams.Count.ShouldBe(32);
        data.Colleges.Colleges.Count.ShouldBe(230);
    }

    [Fact]
    public void The_app_registers_one_shared_instance()
    {
        using var temp = new TempFolder();
        using var services = TestSave.BuildServices(temp.Path);

        var seedData = services.GetRequiredService<ISeedData>();

        seedData.ShouldBeOfType<EmbeddedSeedData>();
        services.GetRequiredService<ISeedData>().ShouldBeSameAs(seedData);   // loaded once per app run
    }

    [Theory]
    [InlineData("""{"name":"T","colleges":[{"name":"A","region":"West","tier":"Power","motto":"x"}]}""")]  // an unknown field
    [InlineData("""{"name":"T","colleges":[{"name":"A","region":"West"}]}""")]                             // a missing field
    [InlineData("""{"name":"T","colleges":[{"name":null,"region":"West","tier":"Power"}]}""")]             // null text
    [InlineData("""{"name":"T","colleges":[{"name":"A","region":"West","tier":2}]}""")]                    // a number for an enum
    [InlineData("""{"name":"T","colleges":[{"name":"A","region":"Midatlantic","tier":"Power"}]}""")]       // an unknown enum name
    public void Strict_parsing_rejects_what_a_typo_produces(string json) =>
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<CollegeList>(json, EmbeddedSeedData.JsonOptions));

    [Fact]
    public void A_field_added_later_with_a_default_still_reads_an_older_file() =>
        JsonSerializer.Deserialize<LaterFormat>("""{"name":"A"}""", EmbeddedSeedData.JsonOptions)!.Motto.ShouldBeNull();
}

/// <summary>A format that grew a field after files were written (D-077).</summary>
internal sealed record LaterFormat(string Name, string? Motto = null);
