using Bunit;
using GridironFrontOffice.UI.Components.Icons;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GridironFrontOffice.UI.Components.Tests.Components;

public sealed class SegmentedControlTests : GfoTestContext
{
    private static readonly IReadOnlyList<SegmentedOption<string>> Modes =
        [new("Dark", "Dark", IconName.WeatherMoon), new("Light", "Light"), new("Match", "Match Windows")];

    [Fact]
    public void It_is_a_labelled_radio_group_with_the_value_checked_and_the_only_one_in_the_Tab_order()
    {
        var cut = Render(Modes, value: "Light");

        var group = cut.Find("[role=radiogroup]");
        group.GetAttribute("aria-label").ShouldBe("Theme");
        var options = cut.FindAll("[role=radio]");
        options.Select(o => o.TextContent.Trim()).ShouldBe(["Dark", "Light", "Match Windows"]);
        options.Select(o => o.GetAttribute("aria-checked")).ShouldBe(["false", "true", "false"]);
        options.Select(o => o.GetAttribute("tabindex")).ShouldBe(["-1", "0", "-1"]);
    }

    [Fact]
    public void Clicking_an_option_chooses_it()
    {
        string? chosen = null;
        var cut = Render(Modes, value: "Dark", changed: v => chosen = v);

        cut.FindAll("[role=radio]")[2].Click();

        chosen.ShouldBe("Match");
        cut.FindAll("[role=radio]")[2].GetAttribute("aria-checked").ShouldBe("true");
    }

    [Theory]
    [InlineData("Dark", "ArrowRight", "Light")]
    [InlineData("Dark", "ArrowLeft", "Match")]     // wraps around
    [InlineData("Match", "ArrowRight", "Dark")]
    [InlineData("Light", "Home", "Dark")]
    [InlineData("Dark", "End", "Match")]
    public void Arrow_keys_and_Home_End_move_the_choice(string from, string key, string expected)
    {
        string? chosen = null;
        var cut = Render(Modes, value: from, changed: v => chosen = v);

        cut.Find("[role=radiogroup]").KeyDown(new KeyboardEventArgs { Key = key });

        chosen.ShouldBe(expected);
    }

    [Fact]
    public void A_disabled_control_ignores_the_keyboard()
    {
        string? chosen = null;
        var cut = Render(Modes, value: "Dark", changed: v => chosen = v, disabled: true);

        cut.Find("[role=radiogroup]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        chosen.ShouldBeNull();
        cut.Find("[role=radiogroup]").GetAttribute("aria-disabled").ShouldBe("true");
    }

    private IRenderedComponent<SegmentedControl<string>> Render(
        IReadOnlyList<SegmentedOption<string>> options, string value, Action<string>? changed = null, bool disabled = false) =>
        Render<SegmentedControl<string>>(p => p
            .Add(c => c.Options, options)
            .Add(c => c.Label, "Theme")
            .Add(c => c.Value, value)
            .Add(c => c.Disabled, disabled)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string>(this, v => changed?.Invoke(v))));
}

public sealed class StatTests : GfoTestContext
{
    [Fact]
    public void A_stat_shows_its_label_value_and_toned_detail()
    {
        var cut = Render<Stat>(p => p.Add(s => s.Label, "Dead money").Add(s => s.Value, "$7.9M").Add(s => s.Detail, "+$4.2M").Add(s => s.Tone, StatTone.Negative));

        cut.Find(".gfo-stat-label").TextContent.ShouldBe("Dead money");
        cut.Find(".gfo-stat-value").TextContent.ShouldBe("$7.9M");
        cut.Find(".gfo-stat-detail").GetAttribute("data-tone").ShouldBe("negative");
    }

    [Fact]
    public void Without_a_detail_there_is_no_detail_line()
    {
        var cut = Render<Stat>(p => p.Add(s => s.Label, "Record").Add(s => s.Value, "4–2"));

        cut.FindAll(".gfo-stat-detail").ShouldBeEmpty();
    }
}

public sealed class EmptyStateTests : GfoTestContext
{
    [Fact]
    public void An_empty_state_shows_its_icon_title_help_and_actions()
    {
        var cut = Render<EmptyState>(p => p
            .Add(e => e.Title, "No saves yet")
            .Add(e => e.Icon, IconName.Folder)
            .AddChildContent("Start a new league.")
            .Add(e => e.Actions, b => b.AddMarkupContent(0, "<button>New league</button>")));

        cut.Find(".gfo-empty-title").TextContent.ShouldBe("No saves yet");
        cut.Find(".gfo-empty-message").TextContent.ShouldBe("Start a new league.");
        cut.Find(".gfo-empty-actions button").TextContent.ShouldBe("New league");
        cut.FindAll(".gfo-empty-icon").Count.ShouldBe(1);
    }

    [Fact]
    public void Title_only_renders_no_empty_parts()
    {
        var cut = Render<EmptyState>(p => p.Add(e => e.Title, "Nothing in the inbox"));

        cut.FindAll(".gfo-empty-icon, .gfo-empty-message, .gfo-empty-actions").ShouldBeEmpty();
    }
}

public sealed class KbdTests : GfoTestContext
{
    [Theory]
    [InlineData("Ctrl+K", new[] { "Ctrl", "K" })]
    [InlineData("Ctrl + Enter", new[] { "Ctrl", "Enter" })]
    [InlineData("G", new[] { "G" })]
    public void Each_key_becomes_a_keycap(string keys, string[] caps)
    {
        var cut = Render<Kbd>(p => p.Add(k => k.Keys, keys));

        cut.FindAll("kbd").Select(k => k.TextContent).ShouldBe(caps);
    }
}
