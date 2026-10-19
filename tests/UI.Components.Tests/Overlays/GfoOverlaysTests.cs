using Bunit;
using GridironFrontOffice.UI.Components.Overlays;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Tests.Overlays;

/// <summary>
/// The thin GFO services over Fluent's dialog and toast services, rendered through Fluent's real providers (its
/// interfaces have internal members, so they can't be faked). This also proves Fluent's argument order end to end.
/// </summary>
public sealed class GfoOverlaysTests : GfoTestContext
{
    [Theory]
    [InlineData("Release", true)]
    [InlineData("Keep", false)]
    public async Task Confirm_shows_the_question_with_the_confirm_button_first_and_returns_the_answer(string click, bool expected)
    {
        var providers = Render<FluentProviders>();
        var dialogs = Services.GetRequiredService<IGfoDialogs>();

        Task<bool> answer = null!;
        // Start the question without awaiting it here: the answer only comes once a button is clicked below.
        await providers.InvokeAsync(() => { answer = dialogs.ConfirmAsync("Release player", "Release M. Okafor?", confirm: "Release", cancel: "Keep"); });

        providers.Find("fluent-dialog [slot=title]").TextContent.ShouldBe("Release player");
        providers.Find("fluent-dialog .message").TextContent.ShouldBe("Release M. Okafor?");
        var buttons = providers.FindAll("fluent-dialog [slot=action] fluent-button");
        buttons.Select(b => b.TextContent.Trim()).ShouldBe(["Release", "Keep"]);
        buttons[0].GetAttribute("appearance").ShouldBe("primary");

        await buttons.Single(b => b.TextContent.Trim() == click).ClickAsync(new());

        (await answer.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken)).ShouldBe(expected);
    }

    [Fact]
    public async Task Peek_opens_a_sheet_with_the_title_and_the_component_with_its_parameters()
    {
        var providers = Render<FluentProviders>();
        var dialogs = Services.GetRequiredService<IGfoDialogs>();

        _ = providers.InvokeAsync(() => dialogs.PeekAsync<PeekSheet>("M. Okafor · EDGE", new Dictionary<string, object?> { [nameof(PeekSheet.PlayerID)] = 7 }, width: "440px"));

        await providers.WaitForAssertionAsync(() => providers.Find("[data-testid=sheet]").TextContent.ShouldBe("Player 7"));
        providers.Markup.ShouldContain("M. Okafor · EDGE");
        providers.Markup.ShouldContain("440px");
    }

    [Theory]
    [InlineData("Success", "success")]
    [InlineData("Info", "info")]
    [InlineData("Warning", "warning")]
    [InlineData("Error", "error")]
    public async Task Each_toast_shows_its_intent_title_and_message(string kind, string intent)
    {
        var providers = Render<FluentProviders>();
        var toasts = Services.GetRequiredService<IGfoToasts>();

        await providers.InvokeAsync(() => kind switch
        {
            "Success" => toasts.SuccessAsync("Signed R. Mensah", "4 years · $38.0M"),
            "Info" => toasts.InfoAsync("Signed R. Mensah", "4 years · $38.0M"),
            "Warning" => toasts.WarningAsync("Signed R. Mensah", "4 years · $38.0M"),
            _ => toasts.ErrorAsync("Signed R. Mensah", "4 years · $38.0M"),
        });

        var toast = providers.Find("fluent-toast-b");
        toast.GetAttribute("intent").ShouldBe(intent);
        toast.QuerySelector("[slot=title]")!.TextContent.ShouldBe("Signed R. Mensah");
        toast.TextContent.ShouldContain("4 years · $38.0M");
    }
}
