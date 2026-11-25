using AngleSharp.Dom;
using Bunit;
using GridironFrontOffice.UI.League;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Tests.League;

/// <summary>
/// S-002: the League page at /league, on a real seed-42 save. Both conferences, each with its four division cards,
/// every team its crest and a link to its page, all in the order <c>GetStructureAsync</c> reads them (the team set's).
/// Criterion 5 (one column under 1,100 px) is checked in the pane, not here.
/// </summary>
public sealed class LeaguePageTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
	[Fact]
	public async Task Two_conferences_of_four_divisions_of_four_teams_in_the_team_sets_order()   // S-002 #1
	{
		var cut = await OpenPageAsync();
		var structure = await Queries.GetStructureAsync(Ct);

		var conferences = cut.FindAll("[data-testid=conference]");
		conferences.Count.ShouldBe(2);
		for (var c = 0; c < conferences.Count; c++)
		{
			var conference = structure.Conferences[c];
			Squash(conferences[c].TextContent).ShouldStartWith(conference.Name, Case.Sensitive, "Each column opens with its conference's name.");

			// Both conferences have an East, a North, a South and a West: each division is checked inside its own column.
			var divisions = conferences[c].QuerySelectorAll("[data-testid=division]");
			divisions.Length.ShouldBe(4, conference.Name);
			for (var d = 0; d < divisions.Length; d++)
			{
				var division = conference.Divisions[d];
				Squash(divisions[d].TextContent).ShouldStartWith(division.Name, Case.Sensitive, $"{conference.Name}'s card {d + 1}");
				divisions[d].QuerySelectorAll("a.gfo-team-link").Select(a => a.GetAttribute("href"))
					.ShouldBe(division.Teams.Select(t => $"/team/{t.TeamID.Value}"), $"{conference.Name} {division.Name}");
			}
		}
	}

	[Fact]
	public async Task Each_team_is_its_crest_and_name_as_a_link_to_its_page()   // S-002 #2
	{
		var cut = await OpenPageAsync();
		var teams = (await Queries.GetStructureAsync(Ct)).Conferences.SelectMany(c => c.Divisions).SelectMany(d => d.Teams).ToList();

		var links = cut.FindAll("[data-testid=division] a.gfo-team-link");

		links.Count.ShouldBe(32);
		for (var i = 0; i < links.Count; i++)
		{
			var team = teams[i];
			links[i].GetAttribute("href").ShouldBe($"/team/{team.TeamID.Value}");
			Words(links[i]).ShouldBe($"{team.City} {team.Nickname}");
			var crest = links[i].QuerySelector("svg.gfo-team-mark").ShouldNotBeNull($"{team.Nickname}: the crest goes inside the link.");
			crest.GetAttribute("width").ShouldBe("24", team.Nickname);
			crest.GetAttribute("aria-hidden").ShouldBe("true", "The name beside it already says which team it is.");
		}
	}

	[Fact]
	public async Task The_heading_is_League()   // S-002 #3
	{
		var cut = await OpenPageAsync();

		var heading = cut.FindAll("h1").ShouldHaveSingleItem();
		Squash(heading.TextContent).ShouldBe("League");
	}

	[Fact]
	public async Task Without_an_open_league_it_goes_to_the_main_menu()   // S-002 #4
	{
		GoTo("/league");

		var cut = Render<LeaguePage>();

		await cut.WaitForAssertionAsync(() => Location.ShouldBe(""), Bound);
	}

	/// <summary>Seed 42 with team 5 picked, on the League page once its read has rendered all eight division cards.</summary>
	private async Task<IRenderedComponent<LeaguePage>> OpenPageAsync()
	{
		await OpenLeagueAsync(seed.League);
		await PickAsync(5);
		GoTo("/league");
		var cut = Render<LeaguePage>();
		await cut.WaitForAssertionAsync(() => cut.FindAll("[data-testid=division]").Count.ShouldBe(8), Bound);   // read off the thread
		return cut;
	}

	/// <summary>The link's own words, without the crest's monogram (an SVG &lt;text&gt; inside the link).</summary>
	private static string Words(IElement link) => Squash(string.Concat(link.ChildNodes.OfType<IText>().Select(t => t.Data)));

	private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
