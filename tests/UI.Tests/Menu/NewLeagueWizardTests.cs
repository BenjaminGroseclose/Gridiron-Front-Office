using System.Globalization;
using Bunit;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Tests.Menu;

/// <summary>
/// M1 WP8 slice 1 (guide §4): New League's first step on the real app (D-093). Generate runs the real CreateLeague,
/// about half a second a league, and the clock reads <see cref="RealScreenContext.ThisYear"/>.
/// </summary>
public sealed class NewLeagueWizardTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    [Fact]
    public async Task The_seed_field_opens_with_a_random_seed_and_Generate_makes_that_league()
    {
        var cut = Render<NewLeaguePage>();
        var shown = Field(cut, "league-seed").Value;

        SeedText.Read(shown).ShouldNotBeNull().ShouldBeInRange(1UL, SeedText.RandomMax);
        await TypeAsync(cut, "league-name", "Saturday League");
        await cut.Find("[data-testid=generate]").ClickAsync(new());

        var header = await HeaderAsync();
        (header.LeagueName, header.Seed).ShouldBe(("Saturday League", ulong.Parse(shown!, CultureInfo.InvariantCulture)));
        Location.ShouldBe("pick-team");
    }

    [Fact]
    public async Task Generate_shows_its_steps_then_opens_the_picker_on_the_typed_seed_and_this_year()
    {
        var cut = Render<NewLeaguePage>();
        var steps = new List<string>();
        cut.OnMarkupUpdated += (_, _) => steps.AddRange(cut.FindAll("[data-testid=step]").Select(s => s.TextContent));
        await TypeAsync(cut, "league-name", "  Saturday League ");
        await TypeAsync(cut, "league-seed", " 123456789 ");

        await cut.Find("[data-testid=generate]").ClickAsync(new());

        steps.Distinct().ShouldBe(["Generating the league", "Saving it"]);
        var header = await HeaderAsync();
        (header.LeagueName, header.Seed, header.Season, header.UserTeam).ShouldBe(("Saturday League", 123456789UL, ThisYear, null));
        SaveFiles().ShouldHaveSingleItem();
        Location.ShouldBe("pick-team");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_refused_name_is_explained_under_its_field_and_nothing_is_saved(string name)
    {
        var cut = Render<NewLeaguePage>();
        await TypeAsync(cut, "league-name", name);

        await cut.Find("[data-testid=generate]").ClickAsync(new());

        (Field(cut, "league-name").Message, Field(cut, "league-name").MessageState).ShouldBe(("A league needs a name.", MessageState.Error));
        Field(cut, "league-seed").MessageState.ShouldBeNull("The seed was fine.");
        NothingSaved();
    }

    [Theory]
    [InlineData("")]        // a cleared field (D-097 #2)
    [InlineData("12a")]
    [InlineData("-1")]
    public async Task A_refused_seed_is_explained_under_its_field_and_nothing_is_saved(string text)
    {
        var cut = Render<NewLeaguePage>();
        await TypeAsync(cut, "league-name", "Saturday League");
        await TypeAsync(cut, "league-seed", text);

        await cut.Find("[data-testid=generate]").ClickAsync(new());

        (Field(cut, "league-seed").Message, Field(cut, "league-seed").MessageState).ShouldBe((SeedText.Refused, MessageState.Error));
        Field(cut, "league-name").MessageState.ShouldBeNull("The name was fine.");
        NothingSaved();
    }

    [Fact]
    public async Task A_corrected_field_loses_its_message_on_the_next_Generate()
    {
        var cut = Render<NewLeaguePage>();
        await cut.Find("[data-testid=generate]").ClickAsync(new());
        await TypeAsync(cut, "league-name", "Saturday League");

        await cut.Find("[data-testid=generate]").ClickAsync(new());

        Field(cut, "league-name").MessageState.ShouldBeNull();
        Location.ShouldBe("pick-team");
    }

    [Fact]
    public void Each_visit_opens_with_no_name_and_its_own_seed()
    {
        var first = Render<NewLeaguePage>();
        var second = Render<NewLeaguePage>();

        Field(first, "league-name").Value.ShouldBeNullOrEmpty();
        Field(first, "league-seed").Value.ShouldNotBe(Field(second, "league-seed").Value, "Each visit draws its own seed (D-097 #2).");
    }

    [Fact]
    public void Back_goes_to_the_main_menu()
    {
        var cut = Render<NewLeaguePage>();

        cut.Find("[data-testid=back]").Click();

        Location.ShouldBe("");
    }

    [Fact]
    public async Task With_a_league_open_it_goes_where_the_league_lands()
    {
        await OpenLeagueAsync(seed.League);
        GoTo("/new");

        var cut = Render<NewLeaguePage>();

        await cut.WaitForAssertionAsync(() => Location.ShouldBe("pick-team"), Bound);
    }

    private static FluentTextInput Field(IRenderedComponent<NewLeaguePage> cut, string testID) =>
        cut.FindComponents<FluentTextInput>().Single(f => f.Instance.AdditionalAttributes?["data-testid"] as string == testID).Instance;

    // What a change event in the field does: the bound value is set before Generate is clicked
    private static Task TypeAsync(IRenderedComponent<NewLeaguePage> cut, string testID, string text) =>
        cut.InvokeAsync(() => Field(cut, testID).ValueChanged.InvokeAsync(text));

    private void NothingSaved()
    {
        Saves.Current.ShouldBeNull();
        SaveFiles().ShouldBeEmpty();
        Location.ShouldBe("");
    }
}
