using System.Globalization;
using Bunit;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.UI.Components.Formatting;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Tests.Menu;

/// <summary>
/// M1 WP8 slice 1 (guide §4): New League's summary on a real seed-42 save (D-099). The league and the marked team, the
/// settings' placeholder, and Take over, the one place the pick is made.
/// </summary>
public sealed class FinalizeTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    private const int Team = 12;

    [Fact]
    public async Task The_summary_names_the_league_its_season_and_seed_and_the_team()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderSummaryAsync(Team);
        var team = await SummaryAsync(Team);

        cut.Find("h1#finalize-title").TextContent.ShouldBe("Ready to take over");
        (Text(cut, "hero-league"), Text(cut, "hero-season"), Text(cut, "hero-seed")).ShouldBe(("Seed 42", "2026", "42"));

        var card = cut.Find("[data-testid=finalize-team]");
        card.QuerySelector(".gfo-finalize-name")!.TextContent.ShouldBe($"{team.Identity.City} {team.Identity.Nickname}");
        card.QuerySelector(".gfo-finalize-division")!.TextContent.ShouldBe($"{team.ConferenceName} · {team.DivisionName}");
        var facts = card.QuerySelectorAll(".gfo-finalize-facts > div")
            .ToDictionary(f => f.QuerySelector("dt")!.TextContent, f => f.QuerySelector("dd")!.TextContent.Trim());
        facts.ShouldBe(new Dictionary<string, string>
        {
            ["OVR"] = Shown(team.RosterStrength.Overall),
            ["OFF"] = Shown(team.RosterStrength.Offense),
            ["DEF"] = Shown(team.RosterStrength.Defense),
            ["Cap space"] = Money.Format(team.CapSpace),
            ["Stadium"] = team.StadiumName,
        });
        card.QuerySelectorAll(".gfo-tier-chip").Length.ShouldBe(3, "Roster strength is three rating chips.");
    }

    [Fact]
    public async Task League_settings_holds_its_place_until_the_first_setting_arrives()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderSummaryAsync(Team);

        cut.Find("h2#finalize-settings-title").TextContent.ShouldBe("League settings");
        var placeholder = cut.Find("[data-testid=finalize-settings]");
        placeholder.QuerySelector(".gfo-empty-title")!.TextContent.ShouldBe("Nothing to set yet");
        placeholder.TextContent.ShouldContain("Injury frequency, Ironman, the clock and more arrive in later versions.");
    }

    [Fact]
    public async Task Take_over_names_the_team_picks_it_and_opens_home()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderSummaryAsync(Team);
        var team = await SummaryAsync(Team);
        var takeOver = cut.Find("[data-testid=take-over]");

        takeOver.TextContent.Trim().ShouldBe($"Take over the {team.Identity.City} {team.Identity.Nickname}");
        (await HeaderAsync()).UserTeam.ShouldBeNull("Nothing is picked before Take over.");

        await takeOver.ClickAsync(new());

        (await HeaderAsync()).UserTeam.ShouldNotBeNull().Identity.TeamID.ShouldBe(new TeamID(Team));
        Location.ShouldBe("league");
    }

    [Fact]
    public async Task Back_returns_to_the_picker_with_the_team_marked_and_picks_nothing()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderSummaryAsync(Team);

        await cut.Find("[data-testid=back]").ClickAsync(new());

        Location.ShouldBe($"pick-team?team={Team}");
        (await HeaderAsync()).UserTeam.ShouldBeNull();
    }

    [Theory]
    [InlineData("/finalize")]
    [InlineData("/finalize?team=99")]
    [InlineData("/finalize?team=0")]
    public async Task Without_a_known_team_it_goes_to_the_picker(string uri)
    {
        await OpenLeagueAsync(seed.League);
        GoTo(uri);

        var cut = Render<Finalize>();

        await cut.WaitForAssertionAsync(() => Location.ShouldBe("pick-team"), Bound);
        cut.FindAll("[data-testid=finalize-team]").ShouldBeEmpty();
    }

    [Fact]
    public async Task A_league_with_a_team_goes_home()
    {
        await OpenLeagueAsync(seed.League);
        await PickAsync(5);
        GoTo($"/finalize?team={Team}");

        var cut = Render<Finalize>();

        await cut.WaitForAssertionAsync(() => Location.ShouldBe("league"), Bound);
        (await HeaderAsync()).UserTeam!.Identity.TeamID.ShouldBe(new TeamID(5), "No second pick.");
    }

    [Fact]
    public void Without_a_league_it_goes_to_the_main_menu()
    {
        GoTo($"/finalize?team={Team}");

        Render<Finalize>();

        Location.ShouldBe("");
    }

    private async Task<IRenderedComponent<Finalize>> RenderSummaryAsync(int team)
    {
        GoTo($"/finalize?team={team}");
        var cut = Render<Finalize>();
        await cut.WaitForElementAsync("[data-testid=finalize-team]", Bound);   // read off the thread
        return cut;
    }

    private async Task<TeamSummaryDto> SummaryAsync(int team) =>
        (await Queries.GetTeamSummaryAsync(new TeamID(team), Ct)).ShouldNotBeNull();

    private static string Shown(PerceivedRating rating) =>
        OverallCalculator.Display(rating.Estimate).ToString(CultureInfo.InvariantCulture);

    private static string Text(IRenderedComponent<Finalize> cut, string testID) => cut.Find($"[data-testid={testID}]").TextContent;
}
