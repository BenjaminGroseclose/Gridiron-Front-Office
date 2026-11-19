using Bunit;
using GridironFrontOffice.UI.Home;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Tests.Home;

/// <summary>
/// S-001 #2: Home at /home takes over LeagueHome's content and its two redirects. Home's own content is S-004's.
/// </summary>
public sealed class HomePageTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
	[Fact]
	public async Task It_names_the_league_and_says_what_comes_next()
	{
		await OpenLeagueAsync(seed.League);
		await PickAsync(5);
		GoTo("/home");

		var cut = Render<HomePage>();

		cut.Find("h1").TextContent.ShouldBe("Seed 42");
		cut.Find("[data-testid=league-empty]").TextContent.ShouldContain("Your league will live here");
		await cut.WaitForStateAsync(() => cut.RenderCount > 1, Bound);   // ComponentBase renders again once OnInitializedAsync is done
		Location.ShouldBe("home", "A league with a team stays home.");
	}

	[Fact]
	public async Task A_league_without_a_team_goes_to_the_picker()
	{
		await OpenLeagueAsync(seed.League);
		GoTo("/home");

		var cut = Render<HomePage>();

		await cut.WaitForAssertionAsync(() => Location.ShouldBe("pick-team"), Bound);
	}

	[Fact]
	public void Without_an_open_league_it_goes_to_the_main_menu()
	{
		GoTo("/home");

		var cut = Render<HomePage>();

		Location.ShouldBe("");
		cut.Markup.Trim().ShouldBeEmpty();
	}
}
