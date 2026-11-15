using AngleSharp.Dom;
using Bunit;
using GridironFrontOffice.UI.Shell;
using GridironFrontOffice.UI.State;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.Shell;

/// <summary>
/// M1 WP9 slice 1 (guide §4, D-102 #2): the top bar on a real seed-42 save. MainLayout follows GameStateStore, so once
/// the store has your team the brand is its crest and name, a link to its Team page, and once it has the header the
/// context is "League · season · phase". Until then the brand is empty (no wordmark that a beat later turns into the
/// crest) and the context is the league's name.
/// </summary>
public sealed class MainLayoutTopBarTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
	/// <summary>What seed 42's header reads as the context: its save name, its first season and phase, in words.</summary>
	private const string Context = "Seed 42 · 2026 · Training Camp";

	[Fact]
	public async Task A_pick_puts_your_teams_crest_and_name_in_the_brand_as_a_link_to_its_page()
	{
		var cut = RenderLayout();
		await OpenLeagueAsync(seed.League);

		await PickAsync(5);

		await cut.WaitForElementAsync("[data-testid=user-team]", Bound);
		var team = (await HeaderAsync()).UserTeam.ShouldNotBeNull().Identity;
		var link = cut.Find(".gfo-appbar-brand [data-testid=user-team] a");
		link.GetAttribute("href").ShouldBe("/team/5");
		Words(link).ShouldBe($"{team.City} {team.Nickname}");
		var crest = link.QuerySelector("svg.gfo-team-mark").ShouldNotBeNull("The crest goes inside the link, before the name.");
		crest.GetAttribute("width").ShouldBe("20");
		crest.GetAttribute("aria-hidden").ShouldBe("true", "The name beside it already says which team it is.");
		cut.FindAll(".gfo-wordmark").ShouldBeEmpty();
		ContextOf(cut).ShouldBe(Context);
	}

	[Fact]
	public async Task Before_the_pick_the_brand_is_empty_and_the_context_names_the_league_season_and_phase()
	{
		var cut = RenderLayout();

		await OpenLeagueAsync(seed.League);

		await cut.WaitForAssertionAsync(() => ContextOf(cut).ShouldBe(Context), Bound);   // the store has the header
		cut.Find(".gfo-appbar-brand").ChildElementCount.ShouldBe(0, "No team yet, and no wordmark either.");
	}

	[Fact]
	public async Task A_layout_made_after_the_store_loaded_starts_with_your_team()
	{
		var store = Services.GetRequiredService<GameStateStore>();   // made first, so it hears the open and the pick
		await OpenLeagueAsync(seed.League);
		await PickAsync(5);
#pragma warning disable VSTHRD003 // The store's own load, which it starts and completes; nothing here needs a synchronization context.
		await store.Loading.WaitAsync(Bound, Ct);
#pragma warning restore VSTHRD003

		var cut = RenderLayout();

		// No wait: the store won't raise Changed again, so the first render has to read what it already holds.
		cut.Find("[data-testid=user-team] a").GetAttribute("href").ShouldBe("/team/5");
		ContextOf(cut).ShouldBe(Context);
	}

	[Fact]
	public async Task Closing_the_league_takes_your_team_out_of_the_top_bar()
	{
		var cut = RenderLayout();
		await OpenLeagueAsync(seed.League);
		await PickAsync(5);
		await cut.WaitForElementAsync("[data-testid=user-team]", Bound);

		await Saves.CloseAsync(Ct);

		await cut.WaitForAssertionAsync(() => cut.FindAll("nav.gfo-shell-nav").ShouldBeEmpty(), Bound);   // the menu shell
		cut.FindAll("[data-testid=user-team], [data-testid=league-name]").ShouldBeEmpty("The menu's app bar is only the title bar.");
		cut.Find(".gfo-appbar-brand").ChildElementCount.ShouldBe(0);
	}

	[Fact]
	public async Task A_layout_that_goes_away_stops_following_the_store()
	{
		var cut = RenderLayout();
		var store = Services.GetRequiredService<GameStateStore>();
		var following = ChangedHandlers(store);

		cut.Instance.Dispose();

		// The layout's own handler goes; AppearanceSync's, inside it, stays until bUnit disposes it.
		(following - ChangedHandlers(store)).ShouldBe(1);
	}

	private IRenderedComponent<MainLayout> RenderLayout() =>
		Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"page\">Page</p>"))));

	private static string ContextOf(IRenderedComponent<MainLayout> cut) => Squash(cut.Find("[data-testid=league-name]").TextContent);

	/// <summary>The link's own words, without the crest's monogram (an SVG &lt;text&gt; inside the link).</summary>
	private static string Words(IElement link) => Squash(string.Concat(link.ChildNodes.OfType<IText>().Select(t => t.Data)));

	private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	/// <summary>
	/// How many handlers <see cref="GameStateStore.Changed"/> holds. A field-like event has no public count, so this reads
	/// its backing field: a handler left behind keeps a dead layout alive and re-rendering for the rest of the session.
	/// </summary>
	private static int ChangedHandlers(GameStateStore store) =>
		(typeof(GameStateStore).GetField(nameof(GameStateStore.Changed), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
			.GetValue(store) as Action)?.GetInvocationList().Length ?? 0;
}
