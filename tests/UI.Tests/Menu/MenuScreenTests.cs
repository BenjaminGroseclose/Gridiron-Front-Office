using System.Globalization;
using Bunit;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.UI.Football;
using GridironFrontOffice.UI.League;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Pages;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.Menu;

public sealed class MainMenuTests : SaveScreenContext
{
    [Fact]
    public void With_no_saves_Continue_waits_and_the_other_choices_are_there()
    {
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=continue]").HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("[data-testid=continue-target]").ShouldBeEmpty();
        cut.FindAll("[data-testid=new-league], [data-testid=load], [data-testid=settings]").Count.ShouldBe(3);
        cut.FindAll("[data-testid=load] [slot=end]").ShouldBeEmpty("No count until there is a save.");
    }

    [Fact]
    public void Exit_asks_the_host_to_quit()
    {
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=exit]").Click();

        Platform.Quits.ShouldBe(1);
    }

    [Fact]
    public void Load_says_how_many_saves_there_are()
    {
        Catalog.Add("Older League", daysAgo: 3);
        Catalog.Add("Saturday League", daysAgo: 1);
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=load] [slot=end]").TextContent.Trim().ShouldBe("2 saves");
    }

    [Fact]
    public void The_short_version_sits_under_the_menu()
    {
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=version]").TextContent.ShouldBe("v0.0.1");
    }

    [Fact]
    public void The_title_backdrop_sits_behind_the_panel()
    {
        var cut = Render<MainMenu>();

        var frame = cut.Find(".gfo-menu");
        frame.FirstElementChild!.ClassList.ShouldContain("gfo-title-bg");
        frame.QuerySelector(".gfo-menu-panel")!.CompareDocumentPosition(frame.FirstElementChild!).ShouldBe(AngleSharp.Dom.DocumentPositions.Preceding);
    }

    [Fact]
    public async Task Continue_opens_the_most_recently_played_save_that_can_open()
    {
        Catalog.Add("Older League", daysAgo: 3);
        Catalog.Add("Newer Version", daysAgo: 0, SaveCardStatus.FromNewerVersion);
        var latest = Catalog.Add("Saturday League", daysAgo: 1);
        await RealApp.GenerateLeagueAsync();   // what the landing reads: a league without a team
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=continue-target]").TextContent.ShouldContain("Saturday League");
        await cut.Find("[data-testid=continue]").ClickAsync(new());

        Saves.Opened.ShouldBe([latest.FilePath]);
        Location.ShouldBe("pick-team");
    }

    // M1 WP8 slice 3 (F-01 §4): Continue names its league's team, with the crest
    [Fact]
    public void Continue_names_its_team_with_the_crest()
    {
        var card = Catalog.Add("Saturday League");
        Catalog.Cards[^1] = card with { Season = 2026, Phase = Phase.TrainingCamp, Team = SaveCardSamples.Excelsiors };

        var cut = Render<MainMenu>();

        var line = cut.Find("[data-testid=continue-target]");
        line.QuerySelector("span")!.TextContent.ShouldStartWith("Saturday League · New York Excelsiors · last played ");
        line.QuerySelector("[data-testid=continue-crest]")!.GetAttribute("aria-hidden").ShouldBe("true", "The team is named beside it.");
    }

    [Fact]
    public void Continue_without_a_team_names_only_the_league()
    {
        var card = Catalog.Add("Saturday League");
        Catalog.Cards[^1] = card with { Season = 2026, Phase = Phase.TrainingCamp };

        var cut = Render<MainMenu>();

        var line = cut.Find("[data-testid=continue-target]");
        line.QuerySelector("span")!.TextContent.ShouldStartWith("Saturday League · last played ");
        line.QuerySelector("[data-testid=continue-crest]").ShouldBeNull();
    }

    [Fact]
    public void A_save_that_fails_to_open_is_reported_and_the_menu_stays()
    {
        Catalog.Add("Broken League");
        Saves.OpenFailure = SaveErrors.Damaged("Broken-League.gfo", "quick_check failed");
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=continue]").Click();

        var toast = Toasts.Shown.ShouldHaveSingleItem();
        (toast.Kind, toast.Title).ShouldBe(("error", "Couldn't open Broken League"));
        toast.Message!.ShouldContain("damaged");
        Location.ShouldBe("");
        Catalog.Reads.ShouldBe(2, "The list is read again: the file may have changed.");
    }

    [Theory]
    [InlineData("new-league", "new")]
    [InlineData("load", "load")]
    [InlineData("settings", "settings")]
    public void Each_choice_goes_to_its_screen(string button, string location)
    {
        var cut = Render<MainMenu>();

        cut.Find($"[data-testid={button}]").Click();

        Location.ShouldBe(location);
    }

    [Fact]
    public void With_a_league_open_the_menu_goes_to_its_home()
    {
        Saves.Open(new OpenSave(Guid.NewGuid(), "Saturday League", @"C:\Saves\a.gfo", 1, Upgraded: false));

        Render<MainMenu>();

        Location.ShouldBe("league");
    }
}

public sealed class LoadScreenTests : SaveScreenContext
{
    [Fact]
    public void With_no_saves_it_offers_a_new_league()
    {
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=no-saves]").TextContent.ShouldContain("No leagues yet");
        cut.Find("[data-testid=no-saves] [data-testid=new-league]").Click();
        Location.ShouldBe("new");
    }

    [Fact]
    public void Each_card_shows_what_the_save_can_do()
    {
        Catalog.Add("Saturday League");
        Catalog.Add("Old League", status: SaveCardStatus.NeedsUpgrade);
        Catalog.Add("Future League", status: SaveCardStatus.FromNewerVersion);
        Catalog.Add("Stale League", status: SaveCardStatus.FromOlderBuild);
        Catalog.Add("broken", status: SaveCardStatus.Unreadable, problem: "This isn't a Gridiron Front Office save.");
        var cut = Render<LoadScreen>();

        var cards = cut.FindAll("[data-testid=save-card]");
        cards.Select(c => c.QuerySelector("h2")!.TextContent).ShouldBe(["Saturday League", "Old League", "Future League", "Stale League", "broken.gfo"]);
        cards.Select(Buttons).ShouldBe(["delete open", "delete open", "show-in-folder delete", "show-in-folder delete", "show-in-folder delete"]);
        cards[0].QuerySelector("[data-testid=save-note]").ShouldBeNull();
        cards[1].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("older version");
        cards[2].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("Update Gridiron Front Office");
        cards[3].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("older development build");
        cards[4].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("isn't a Gridiron Front Office save");

        cards[0].QuerySelector("[data-testid=delete]")!.ClassList.ShouldContain("gfo-danger", "Delete destroys the save: the danger button.");

        static string Buttons(AngleSharp.Dom.IElement card) =>
            string.Join(' ', card.QuerySelectorAll("[data-testid]").Select(b => b.GetAttribute("data-testid")).Where(id => id is "open" or "delete" or "show-in-folder"));
    }

    // M1 WP8 slice 3 (F-01 §4): a card shows its team's crest and the league line; a league without a team says so
    [Fact]
    public void A_card_names_its_team_season_and_phase()
    {
        var picked = Catalog.Add("Saturday League");
        Catalog.Cards[^1] = picked with { Season = 2026, Phase = Phase.TrainingCamp, Team = SaveCardSamples.Excelsiors };
        var unpicked = Catalog.Add("Sunday League", daysAgo: 1);
        Catalog.Cards[^1] = unpicked with { Season = 2026, Phase = Phase.TrainingCamp };
        Catalog.Add("Old League", daysAgo: 2, status: SaveCardStatus.NeedsUpgrade);

        var cut = Render<LoadScreen>();

        var cards = cut.FindAll("[data-testid=save-card]");
        cards.Select(c => c.QuerySelector("[data-testid=save-league]")?.TextContent)
            .ShouldBe(["New York Excelsiors · 2026 · Training Camp", "No team picked yet", null]);
        cards.Select(c => c.QuerySelector("[data-testid=save-crest]") is not null).ShouldBe([true, false, false]);
        cards[0].QuerySelector("[data-testid=save-crest]")!.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public async Task Open_lands_the_league_and_says_when_the_file_was_upgraded()
    {
        var card = Catalog.Add("Old League", status: SaveCardStatus.NeedsUpgrade);
        Saves.OpenUpgrades = true;
        await RealApp.GenerateLeagueAsync();   // what the landing reads: a league without a team
        var cut = Render<LoadScreen>();

        await cut.Find("[data-testid=open]").ClickAsync(new());

        Saves.Opened.ShouldBe([card.FilePath]);
        Location.ShouldBe("pick-team");
        Toasts.Shown.ShouldHaveSingleItem().Title.ShouldBe("Old League was upgraded");
    }

    [Fact]
    public void Delete_asks_first_naming_the_league_and_keeping_it_deletes_nothing()
    {
        Catalog.Add("Saturday League");
        Dialogs.Answer = false;
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=delete]").Click();

        var question = Dialogs.Asked.ShouldHaveSingleItem();
        (question.Title, question.Confirm).ShouldBe(("Delete Saturday League?", "Delete"));
        question.Message.ShouldBe("Saturday League, with its snapshots, goes to the Recycle Bin. You can restore it from there.");
        Saves.Deleted.ShouldBeEmpty();
    }

    [Fact]
    public void An_unreadable_file_is_deleted_without_talk_of_snapshots()
    {
        Catalog.Add("broken", status: SaveCardStatus.Unreadable, problem: "Damaged.");
        Dialogs.Answer = false;
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=delete]").Click();

        Dialogs.Asked.ShouldHaveSingleItem().Message.ShouldBe("broken.gfo goes to the Recycle Bin. You can restore it from there.");
    }

    [Fact]
    public void A_confirmed_delete_removes_the_card()
    {
        var card = Catalog.Add("Saturday League");
        var cut = Render<LoadScreen>();
        Catalog.Cards.Clear();   // what the folder holds once the file has gone

        cut.Find("[data-testid=delete]").Click();

        Saves.Deleted.ShouldBe([card.FilePath]);
        Toasts.Shown.ShouldHaveSingleItem().ShouldBe(("success", "Saturday League moved to the Recycle Bin", (string?)null));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid=save-card]").ShouldBeEmpty());
    }

    [Fact]
    public void A_delete_the_player_cancels_in_Windows_is_a_notice_not_an_error()
    {
        var card = Catalog.Add("Saturday League");
        Saves.DeleteFailure = SaveErrors.DeleteCancelled(card.FileName);
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=delete]").Click();

        Toasts.Shown.ShouldHaveSingleItem().Kind.ShouldBe("info");
        cut.FindAll("[data-testid=save-card]").Count.ShouldBe(1);
    }

    [Fact]
    public void Show_in_folder_points_at_the_file()
    {
        var card = Catalog.Add("broken", status: SaveCardStatus.Unreadable, problem: "Damaged.");
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=show-in-folder]").Click();

        Platform.ShownInFolder.ShouldBe([card.FilePath]);
    }
}

public sealed class LeagueHomeTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    [Fact]
    public async Task It_names_the_league_and_says_what_comes_next()
    {
        await OpenLeagueAsync(seed.League);
        await PickAsync(5);
        GoTo("/league");

        var cut = Render<LeagueHome>();

        cut.Find("h1").TextContent.ShouldBe("Seed 42");
        cut.Find("[data-testid=league-empty]").TextContent.ShouldContain("Your league will live here");
        await cut.WaitForStateAsync(() => cut.RenderCount > 1, Bound);   // ComponentBase renders again once OnInitializedAsync is done
        Location.ShouldBe("league", "A league with a team stays home.");
    }

    [Fact]
    public async Task A_league_without_a_team_goes_to_the_picker()
    {
        await OpenLeagueAsync(seed.League);
        GoTo("/league");

        var cut = Render<LeagueHome>();

        await cut.WaitForAssertionAsync(() => Location.ShouldBe("pick-team"), Bound);
    }

    [Fact]
    public void Without_an_open_league_it_goes_to_the_main_menu()
    {
        GoTo("/league");

        var cut = Render<LeagueHome>();

        Location.ShouldBe("");
        cut.Markup.Trim().ShouldBeEmpty();
    }
}

/// <summary>M1 WP8 (guide §4): Continue and Load land a real league where LandingAsync says.</summary>
public sealed class MenuLandingTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    [Theory]
    [InlineData(false, "pick-team")]
    [InlineData(true, "league")]
    public async Task Continue_lands_a_league_on_the_picker_until_it_has_a_team(bool picked, string landing)
    {
        await SavedLeagueAsync(picked);
        var cut = Render<MainMenu>();
        await cut.WaitForAssertionAsync(() => cut.Find("[data-testid=continue]").HasAttribute("disabled").ShouldBeFalse(), Bound);   // the list is read off the thread

        await cut.Find("[data-testid=continue]").ClickAsync(new());

        Saves.Current.ShouldNotBeNull().LeagueName.ShouldBe("Seed 42");
        Location.ShouldBe(landing);
    }

    [Theory]
    [InlineData(false, "pick-team")]
    [InlineData(true, "league")]
    public async Task Load_lands_a_league_on_the_picker_until_it_has_a_team(bool picked, string landing)
    {
        await SavedLeagueAsync(picked);
        var cut = Render<LoadScreen>();
        await cut.WaitForElementAsync("[data-testid=open]", Bound);   // the list is read off the thread

        await cut.Find("[data-testid=open]").ClickAsync(new());

        Saves.Current.ShouldNotBeNull().LeagueName.ShouldBe("Seed 42");
        Location.ShouldBe(landing);
    }

    // A league on disk, closed, as the menu finds it
    private async Task SavedLeagueAsync(bool picked)
    {
        await OpenLeagueAsync(seed.League);
        if (picked)
        {
            await PickAsync(5);
        }

        await Saves.CloseAsync(Xunit.TestContext.Current.CancellationToken);
    }
}

public sealed class SaveCardTextTests
{
    [Theory]
    [InlineData(0, "under a minute")]
    [InlineData(59, "under a minute")]
    [InlineData(60, "1 min")]
    [InlineData(2700, "45 min")]
    [InlineData(8100, "2 h 15 min")]
    [InlineData(90061, "25 h 1 min")]
    public void Play_time_reads_naturally(long seconds, string expected) => SaveCardText.PlayTime(seconds).ShouldBe(expected);

    [Fact]
    public void The_league_line_names_the_team_season_and_phase_or_says_there_is_no_team()
    {
        var card = new SaveCard(@"C:\Saves\a.gfo", SaveCardStatus.Ready) { LeagueName = "A", Season = 2027, Phase = Phase.ResignWindow };

        SaveCardText.League(card with { Team = SaveCardSamples.Excelsiors }).ShouldBe("New York Excelsiors · 2027 · Re-sign & Tag Window");
        SaveCardText.League(card).ShouldBe("No team picked yet");
        SaveCardText.League(card with { Season = null, Phase = null }).ShouldBeNull("No league to show: a save from another version, or none in it.");
    }

    [Fact]
    public void An_unreadable_file_is_named_by_its_file_and_has_no_details()
    {
        var card = new SaveCard(@"C:\Saves\broken.gfo", SaveCardStatus.Unreadable) { Problem = "Damaged." };

        SaveCardText.Title(card).ShouldBe("broken.gfo");
        SaveCardText.Details(card, CultureInfo.InvariantCulture).ShouldBeNull();
        SaveCardText.Note(card).ShouldBe("Damaged.");
    }

    [Fact]
    public void A_newer_save_names_the_version_that_made_it()
    {
        var card = new SaveCard(@"C:\Saves\a.gfo", SaveCardStatus.FromNewerVersion) { LeagueName = "A", AppVersionLastOpened = "0.4.0" };

        SaveCardText.Note(card).ShouldBe("Saved by a newer version (0.4.0). Update Gridiron Front Office to open it.");
    }

    [Fact]
    public void A_save_from_an_older_build_says_to_delete_it()
    {
        var card = new SaveCard(@"C:\Saves\a.gfo", SaveCardStatus.FromOlderBuild) { LeagueName = "A" };

        SaveCardText.Note(card).ShouldBe("Saved by an older development build. It can't be opened: delete it.");
    }

    [Fact]
    public void The_details_line_has_both_days_and_the_play_time()
    {
        var card = new SaveCard(@"C:\Saves\a.gfo", SaveCardStatus.Ready)
        {
            LeagueName = "A",
            CreatedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            LastPlayedAtUtc = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc),
            PlayTimeSeconds = 8100,
        };

        // The days are shown in local time; the expected text converts the same way, so any time zone passes.
        static string Day(DateTime utc) => utc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        SaveCardText.Details(card, CultureInfo.InvariantCulture)
            .ShouldBe($"Last played {Day(card.LastPlayedAtUtc)} · Created {Day(card.CreatedAtUtc)} · 2 h 15 min played");
    }
}

public sealed class SettingsFromTheMenuTests : SaveScreenContext
{
    [Fact]
    public void About_shows_the_full_version_for_bug_reports()
    {
        var cut = Render<SettingsPage>();

        cut.Find("[data-testid=version]").TextContent.ShouldBe("0.0.1+94b9338 (Debug)");
    }

    [Fact]
    public void Copy_puts_the_full_version_on_the_clipboard()
    {
        var cut = Render<SettingsPage>();

        cut.Find("[data-testid=copy-version]").Click();

        JSInterop.VerifyInvoke("navigator.clipboard.writeText").Arguments.ShouldBe(["0.0.1+94b9338 (Debug)"]);
        Toasts.Shown.ShouldHaveSingleItem().ShouldBe(("success", "Version copied", (string?)null));
    }

    [Fact]
    public void A_clipboard_that_refuses_says_so()
    {
        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true).SetException(new Microsoft.JSInterop.JSException("Write permission denied."));
        var cut = Render<SettingsPage>();

        cut.Find("[data-testid=copy-version]").Click();

        Toasts.Shown.ShouldHaveSingleItem().Kind.ShouldBe("warning");
    }

    [Fact]
    public void At_the_main_menu_Settings_offers_the_way_back()
    {
        var cut = Render<SettingsPage>();

        cut.Find("[data-testid=back]").Click();

        Location.ShouldBe("");
    }

    [Fact]
    public void Inside_a_league_the_navigation_column_does_that_instead()
    {
        Saves.Open(new OpenSave(Guid.NewGuid(), "Saturday League", @"C:\Saves\a.gfo", 1, Upgraded: false));

        var cut = Render<SettingsPage>();

        cut.FindAll("[data-testid=back]").ShouldBeEmpty();
    }
}

/// <summary>CALENDAR_AND_PHASES.md §2: the phases as the screens name them (M1 WP8 slice 3).</summary>
public sealed class PhasesTests
{
    [Fact]
    public void Every_phase_has_a_name_of_its_own()
    {
        var names = Enum.GetValues<Phase>().Select(p => p.Text()).ToList();

        names.ShouldAllBe(n => n.Length > 0);
        names.Distinct().Count().ShouldBe(names.Count);
        (Phase.TrainingCamp.Text(), Phase.ResignWindow.Text(), Phase.PreDraft.Text())
            .ShouldBe(("Training Camp", "Re-sign & Tag Window", "Pre-Draft"));
    }
}

/// <summary>A team for cards made by hand: the Excelsiors as the shipped team set has them.</summary>
internal static class SaveCardSamples
{
    public static TeamIdentityDto Excelsiors { get; } = new(new TeamID(1), "NYX", "New York", "Excelsiors", "#A6192E", "#2B2E34",
        "#C9CDD2", CrestShape.Shield, "NY", CrestStripe.TwinBars);
}
