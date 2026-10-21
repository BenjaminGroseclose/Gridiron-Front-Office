using System.Globalization;
using Bunit;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.UI.League;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Pages;
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
    public void Continue_opens_the_most_recently_played_save_that_can_open()
    {
        Catalog.Add("Older League", daysAgo: 3);
        Catalog.Add("Newer Version", daysAgo: 0, SaveCardStatus.FromNewerVersion);
        var latest = Catalog.Add("Saturday League", daysAgo: 1);
        var cut = Render<MainMenu>();

        cut.Find("[data-testid=continue-target]").TextContent.ShouldContain("Saturday League");
        cut.Find("[data-testid=continue]").Click();

        Saves.Opened.ShouldBe([latest.FilePath]);
        Location.ShouldBe("league");
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

public sealed class NewLeaguePageTests : SaveScreenContext
{
    [Fact]
    public async Task Creating_opens_the_new_league_at_its_home()
    {
        var cut = Render<NewLeaguePage>();
        var name = cut.FindComponent<Microsoft.FluentUI.AspNetCore.Components.FluentTextInput>().Instance;

        await cut.InvokeAsync(() => name.ValueChanged.InvokeAsync("Saturday League"));
        await cut.Find("[data-testid=create]").ClickAsync(new());

        Saves.Current!.LeagueName.ShouldBe("Saturday League");
        Location.ShouldBe("league");
    }

    [Fact]
    public void A_refused_name_is_explained_under_the_field_and_nothing_happens()
    {
        var cut = Render<NewLeaguePage>();

        cut.Find("[data-testid=create]").Click();

        Saves.Current.ShouldBeNull();
        Location.ShouldBe("");
        cut.FindComponent<Microsoft.FluentUI.AspNetCore.Components.FluentTextInput>().Instance.Message.ShouldBe("A league needs a name.");
    }

    [Fact]
    public void Main_menu_goes_back()
    {
        var cut = Render<NewLeaguePage>();

        cut.Find("[data-testid=back]").Click();

        Location.ShouldBe("");
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
        Catalog.Add("broken", status: SaveCardStatus.Unreadable, problem: "This isn't a Gridiron Front Office save.");
        var cut = Render<LoadScreen>();

        var cards = cut.FindAll("[data-testid=save-card]");
        cards.Select(c => c.QuerySelector("h2")!.TextContent).ShouldBe(["Saturday League", "Old League", "Future League", "broken.gfo"]);
        cards.Select(Buttons).ShouldBe(["delete open", "delete open", "show-in-folder delete", "show-in-folder delete"]);
        cards[0].QuerySelector("[data-testid=save-note]").ShouldBeNull();
        cards[1].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("older version");
        cards[2].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("Update Gridiron Front Office");
        cards[3].QuerySelector("[data-testid=save-note]")!.TextContent.ShouldContain("isn't a Gridiron Front Office save");

        static string Buttons(AngleSharp.Dom.IElement card) =>
            string.Join(' ', card.QuerySelectorAll("[data-testid]").Select(b => b.GetAttribute("data-testid")).Where(id => id is "open" or "delete" or "show-in-folder"));
    }

    [Fact]
    public void Open_goes_to_the_leagues_home_and_says_when_the_file_was_upgraded()
    {
        var card = Catalog.Add("Old League", status: SaveCardStatus.NeedsUpgrade);
        Saves.OpenUpgrades = true;
        var cut = Render<LoadScreen>();

        cut.Find("[data-testid=open]").Click();

        Saves.Opened.ShouldBe([card.FilePath]);
        Location.ShouldBe("league");
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

public sealed class LeagueHomeTests : SaveScreenContext
{
    [Fact]
    public void It_names_the_league_and_says_what_comes_next()
    {
        Saves.Open(new OpenSave(Guid.NewGuid(), "Saturday League", @"C:\Saves\a.gfo", 1, Upgraded: false));

        var cut = Render<LeagueHome>();

        cut.Find("h1").TextContent.ShouldBe("Saturday League");
        cut.Find("[data-testid=league-empty]").TextContent.ShouldContain("Your league will live here");
    }

    [Fact]
    public void Without_an_open_league_it_goes_to_the_main_menu()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/league");

        var cut = Render<LeagueHome>();

        Location.ShouldBe("");
        cut.Markup.Trim().ShouldBeEmpty();
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
