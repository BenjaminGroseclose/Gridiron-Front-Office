using AngleSharp.Dom;
using Bunit;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.UI.Components.Formatting;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Tests.Menu;

/// <summary>
/// M1 WP8 slice 1 (guide §4): the team picker on a real seed-42 save (D-093), checked against the shipped team set and
/// the league's own summaries. A click marks a card; nothing is picked here (D-095 #3, D-099).
/// </summary>
public sealed class TeamPickerTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    [Fact]
    public async Task The_picker_shows_every_team_by_conference()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();
        var set = Data.TeamSet;

        var conferences = cut.FindAll(".gfo-picker-conference");
        conferences.Select(c => (c.GetAttribute("aria-label"), c.QuerySelector("h2")!.TextContent))
            .ShouldBe(set.Conferences.Select(c => ((string?)c.Name, c.Name)));
        conferences.Select(c => c.QuerySelectorAll("[data-testid=pick-card]").Length).ShouldBe([16, 16]);

        // The team set's order: each conference's divisions in turn
        var expected = set.Conferences.SelectMany(c => c.Divisions).SelectMany(d => d.Teams)
            .Select(abbr => set.Teams.Single(t => t.Abbr == abbr))
            .Select(t => $"{t.City} {t.Nickname}");
        Cards(cut).Select(Name).ShouldBe(expected);
    }

    [Fact]
    public async Task A_card_names_its_team_and_division_with_its_three_ratings_and_cap_space()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();
        var summaries = await Queries.GetTeamSummariesAsync(Ct);

        foreach (var (card, team) in Cards(cut).Zip(summaries.OrderBy(Order)))
        {
            card.QuerySelector("fluent-text")!.TextContent.Trim().ShouldBe($"{team.Identity.City} {team.Identity.Nickname} · {team.DivisionName}");
            card.QuerySelectorAll(".gfo-tier-chip").Select(c => c.TextContent).ShouldBe(
                new[] { team.RosterStrength.Overall, team.RosterStrength.Offense, team.RosterStrength.Defense }
                    .Select(r => OverallCalculator.Display(r.Estimate).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                team.Identity.Abbreviation);
            card.QuerySelector(".gfo-pick-cap .gfo-num")!.TextContent.ShouldBe(Money.Format(team.CapSpace), team.Identity.Abbreviation);
        }

        // The cards' order is the team set's, which groups by conference; the summaries are in TeamID order
        int Order(TeamSummaryDto team) => Cards(cut).Select(Name).ToList().IndexOf($"{team.Identity.City} {team.Identity.Nickname}");
    }

    [Fact]
    public async Task The_hero_band_names_the_league_its_season_and_its_seed()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();

        cut.Find("h1#picker-title").TextContent.ShouldBe("Pick your team");
        (Text(cut, "hero-league"), Text(cut, "hero-season"), Text(cut, "hero-seed")).ShouldBe(("Seed 42", "2026", "42"));
    }

    [Fact]
    public async Task Nothing_is_marked_at_first_and_Next_waits()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();

        Cards(cut).ShouldAllBe(c => c.GetAttribute("aria-pressed") == "false");
        cut.Find("[data-testid=next]").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task A_click_marks_one_card_and_a_second_moves_the_mark_without_picking()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();

        await Cards(cut)[2].ClickAsync(new());
        Marked(cut).ShouldBe([2]);
        await Cards(cut)[9].ClickAsync(new());
        Marked(cut).ShouldBe([9]);

        cut.Find("[data-testid=next]").HasAttribute("disabled").ShouldBeFalse();
        (await HeaderAsync()).UserTeam.ShouldBeNull("Marking picks nothing.");
    }

    [Fact]
    public async Task Next_opens_the_summary_for_the_marked_team()
    {
        await OpenLeagueAsync(seed.League);
        var cut = await RenderPickerAsync();
        await Cards(cut)[9].ClickAsync(new());
        var marked = Name(Cards(cut)[9]);

        await cut.Find("[data-testid=next]").ClickAsync(new());

        var team = (await Queries.GetTeamSummariesAsync(Ct))
            .Single(t => $"{t.Identity.City} {t.Identity.Nickname}" == marked);
        Location.ShouldBe($"finalize?team={team.Identity.TeamID.Value}");
        (await HeaderAsync()).UserTeam.ShouldBeNull("Next picks nothing either.");
    }

    [Fact]
    public async Task A_team_in_the_route_is_marked_on_arrival()
    {
        await OpenLeagueAsync(seed.League);
        GoTo("/pick-team?team=12");

        var cut = await RenderPickerAsync();

        var team = (await Queries.GetTeamSummaryAsync(new Core.IDs.TeamID(12), Ct))!;
        Marked(cut).Select(i => Name(Cards(cut)[i])).ShouldBe([$"{team.Identity.City} {team.Identity.Nickname}"]);
        cut.Find("[data-testid=next]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task A_league_with_a_team_goes_home()
    {
        await OpenLeagueAsync(seed.League);
        await PickAsync(5);
        GoTo("/pick-team");

        var cut = Render<TeamPickerPage>();

        await cut.WaitForAssertionAsync(() => Location.ShouldBe("league"), Bound);
        cut.FindAll("[data-testid=pick-card]").ShouldBeEmpty();
    }

    [Fact]
    public void Without_a_league_it_goes_to_the_main_menu()
    {
        GoTo("/pick-team");

        Render<TeamPickerPage>();

        Location.ShouldBe("");
    }

    [Fact]
    public async Task A_save_with_no_teams_says_so()
    {
        (await Saves.CreateAsync("Empty League", Ct)).IsSuccess.ShouldBeTrue();

        var cut = Render<TeamPickerPage>();

        await cut.WaitForElementAsync("[data-testid=picker-empty]", Bound);
        cut.FindAll("[data-testid=pick-card]").ShouldBeEmpty();
        cut.Find("[data-testid=close-league]");
    }

    private async Task<IRenderedComponent<TeamPickerPage>> RenderPickerAsync()
    {
        var cut = Render<TeamPickerPage>();
        await cut.WaitForAssertionAsync(() => cut.FindAll("[data-testid=pick-card]").Count.ShouldBe(32), Bound);   // read off the thread
        return cut;
    }

    private static IReadOnlyList<IElement> Cards(IRenderedComponent<TeamPickerPage> cut) => cut.FindAll("[data-testid=pick-card]");

    private static int[] Marked(IRenderedComponent<TeamPickerPage> cut) =>
        [.. Cards(cut).Select((c, i) => (c, i)).Where(x => x.c.GetAttribute("aria-pressed") == "true").Select(x => x.i)];

    // "City Nickname" from the card's "City Nickname · Division" (the card's own text also holds the crest's monogram)
    private static string Name(IElement card) => card.QuerySelector("fluent-text")!.TextContent.Split(" · ")[0].Trim();

    private static string Text(IRenderedComponent<TeamPickerPage> cut, string testID) => cut.Find($"[data-testid={testID}]").TextContent;
}
