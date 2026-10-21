using Bunit;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.UI.Shell;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.Shell;

public sealed class MainLayoutTests : SaveScreenContext
{
    private static readonly OpenSave League = new(Guid.NewGuid(), "Saturday League", @"C:\Saves\a.gfo", 1, Upgraded: false);

    [Fact]
    public void At_the_main_menu_the_page_has_the_frame_to_itself()
    {
        var cut = RenderLayout();

        cut.Find("main .page").TextContent.ShouldBe("Page");
        cut.Find("header.gfo-appbar").HasAttribute("data-drag-region").ShouldBeTrue("It's still the title bar.");
        cut.FindAll(".gfo-wordmark").ShouldBeEmpty();
        cut.FindAll("nav.gfo-shell-nav, [data-testid=close-league], [data-testid=league-name]").ShouldBeEmpty();
        cut.Find("main.gfo-shell-content").ClassList.ShouldContain("is-bleed", "The menu screens paint their own backdrop to the edges.");
    }

    [Fact]
    public void With_a_league_open_the_shell_names_it_and_shows_the_navigation()
    {
        Saves.Open(League);

        var cut = RenderLayout();

        cut.Find("[data-testid=league-name]").TextContent.ShouldBe("Saturday League");
        cut.Find("nav.gfo-shell-nav");
        cut.Find("[data-testid=close-league]");
        cut.Find("main .page").TextContent.ShouldBe("Page");
        cut.Find("main.gfo-shell-content").ClassList.ShouldNotContain("is-bleed");
    }

    [Fact]
    public void Close_league_closes_it_and_returns_to_the_main_menu()
    {
        Saves.Open(League);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/league");
        var cut = RenderLayout();

        cut.Find("[data-testid=close-league]").Click();

        Saves.Closes.ShouldBe(1);
        Location.ShouldBe("");
        cut.WaitForAssertion(() => cut.FindAll("nav.gfo-shell-nav").ShouldBeEmpty());
    }

    [Fact]
    public void The_shell_follows_a_league_opened_from_any_screen()
    {
        var cut = RenderLayout();

        Saves.Open(League);

        cut.WaitForAssertion(() => cut.Find("[data-testid=league-name]").TextContent.ShouldBe("Saturday League"));
    }

    [Fact]
    public void A_layout_that_goes_away_stops_listening()
    {
        var cut = RenderLayout();
        var listening = Events.Subscribers;

        cut.Instance.Dispose();

        listening.ShouldBe(2);
        Events.Subscribers.ShouldBe(0);
    }

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"page\">Page</p>"))));
}
