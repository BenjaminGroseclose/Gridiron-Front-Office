using Bunit;
using GridironFrontOffice.UI.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Tests.Shell;

public sealed class PageErrorBoundaryTests : BunitContext
{
    private readonly ListLoggerProvider _logs = new();
    private readonly Thrower.FailureSwitch _failure = new();

    public PageErrorBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // Fluent's web components call into JS
        Services.AddLogging(logging => logging.AddProvider(_logs));
        Services.AddFluentUIComponents();
    }

    [Fact]
    public void A_healthy_page_renders_as_is()
    {
        var cut = RenderBoundary();

        cut.Find("[data-testid=content]").TextContent.ShouldBe("The page content");
        cut.FindAll("[data-testid=page-error]").ShouldBeEmpty();
        _logs.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void A_failing_page_is_logged_and_replaced_by_the_recovery_panel()
    {
        _failure.On = true;

        var cut = RenderBoundary();

        cut.Find("[data-testid=page-error] h1").TextContent.ShouldBe("This screen hit a problem");
        cut.FindAll("[data-testid=content]").ShouldBeEmpty();
        var entry = _logs.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.EventId.ShouldBe(8000);
        entry.Category.ShouldBe(typeof(PageErrorBoundary).FullName);
        entry.Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Test failure while rendering");
    }

    [Fact]
    public void Try_again_renders_the_page_once_the_problem_is_gone()
    {
        _failure.On = true;
        var cut = RenderBoundary();

        _failure.On = false;
        cut.Find("[data-testid=page-error] fluent-button").Click();

        cut.Find("[data-testid=content]").TextContent.ShouldBe("The page content");
    }

    [Fact]
    public void Navigating_away_clears_the_error()
    {
        _failure.On = true;
        var cut = RenderBoundary();

        _failure.On = false;
        Services.GetRequiredService<NavigationManager>().NavigateTo("/roster");

        cut.WaitForAssertion(() => cut.Find("[data-testid=content]").ShouldNotBeNull());
    }

    private IRenderedComponent<PageErrorBoundary> RenderBoundary() =>
        Render<PageErrorBoundary>(parameters => parameters
            .AddChildContent<Thrower>(child => child.Add(t => t.Switch, _failure)));
}
