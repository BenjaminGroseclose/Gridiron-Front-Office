using Bunit;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using GridironFrontOffice.UI.Components.Theming;
using GridironFrontOffice.UI.Pages;
using GridironFrontOffice.UI.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Tests.Shell;

public sealed class AppearanceMappingTests
{
    [Theory]
    [InlineData(ThemeChoice.Dark, GfoThemeMode.Dark)]
    [InlineData(ThemeChoice.Light, GfoThemeMode.Light)]
    [InlineData(ThemeChoice.MatchWindows, GfoThemeMode.MatchWindows)]
    public void The_theme_choice_maps_to_the_design_systems_mode(ThemeChoice choice, GfoThemeMode mode) =>
        AppearanceMapping.From(new AppearanceSettings { Theme = choice }, teamPrimary: null).Mode.ShouldBe(mode);

    [Theory]
    [InlineData(AccentMode.Team, "#1B2A4A", "#1B2A4A")]
    [InlineData(AccentMode.Team, null, null)]          // no league open yet: neutral
    [InlineData(AccentMode.Neutral, "#1B2A4A", null)]  // the player turned team color off
    public void The_accent_is_the_team_color_only_when_wanted_and_known(AccentMode accent, string? team, string? expected) =>
        AppearanceMapping.From(new AppearanceSettings { Accent = accent }, team).AccentColor.ShouldBe(expected);

    [Fact]
    public void Density_carries_over() =>
        AppearanceMapping.From(new AppearanceSettings { Density = Density.Compact }, null).Density.ShouldBe(GfoDensity.Compact);
}

public sealed class AppShellTests : GfoTestContext
{
    [Fact]
    public void The_app_bar_is_the_drag_region_with_the_wordmark_and_a_polite_progress_line()
    {
        Services.AddGfoUi();

        var cut = Render<AppShell>(p => p
            .Add(s => s.Progress, b => b.AddContent(0, "Simulating Week 7 · 9/16 games"))
            .Add(s => s.Navigation, b => b.AddMarkupContent(0, "<a href=\"/\">Home</a>"))
            .AddChildContent("<p class=\"page\">Page</p>"));

        var bar = cut.Find("header.gfo-appbar");
        bar.HasAttribute("data-drag-region").ShouldBeTrue();
        bar.QuerySelector(".gfo-wordmark")!.TextContent.ShouldBe("Gridiron Front Office");
        var progress = cut.Find(".gfo-appbar-progress");
        (progress.GetAttribute("role"), progress.GetAttribute("aria-live")).ShouldBe(("status", "polite"));
        progress.TextContent.ShouldBe("Simulating Week 7 · 9/16 games");
        cut.Find("nav.gfo-shell-nav").GetAttribute("aria-label").ShouldBe("Main");
        cut.Find("main .page").TextContent.ShouldBe("Page");
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void Room_for_the_caption_buttons_only_when_the_app_bar_is_the_title_bar(bool titleBar, int gaps)
    {
        Services.AddSingleton(new ShellOptions(AppBarIsTitleBar: titleBar));
        Services.AddGfoUi();

        var cut = Render<AppShell>();

        cut.FindAll(".gfo-caption-gap").Count.ShouldBe(gaps);
    }

    [Fact]
    public void A_brand_replaces_the_wordmark()
    {
        Services.AddGfoUi();

        var cut = Render<AppShell>(p => p.Add(s => s.Brand, b => b.AddMarkupContent(0, "<span class=\"team\">Wolves</span>")));

        cut.Find(".gfo-appbar-brand .team").TextContent.ShouldBe("Wolves");
        cut.FindAll(".gfo-wordmark").ShouldBeEmpty();
    }
}

public sealed class MainNavTests : GfoTestContext
{
    [Theory]
    [InlineData("/league", "Home")]
    [InlineData("/settings", "Settings")]
    public void Home_and_Settings_with_the_current_one_active(string uri, string active)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

        var cut = Render<MainNav>();

        var items = cut.FindAll(".fluent-navitem");
        items.Select(i => i.TextContent.Trim()).ShouldBe(["Home", "Settings"]);
        items.Single(i => i.ClassList.Contains("gfo-nav-active")).TextContent.Trim().ShouldBe(active);
    }
}

public sealed class AppearanceSyncTests : GfoTestContext
{
    private readonly FakeSettingsStore _settings = new(new AppSettings { Appearance = new AppearanceSettings { Theme = ThemeChoice.Light } });

    public AppearanceSyncTests() => Services.AddSingleton<ISettingsStore>(_settings);

    [Fact]
    public void The_saved_appearance_is_applied_once_the_page_is_up()
    {
        Render<AppearanceSync>();

        Fluent.Created.ShouldHaveSingleItem().Mode.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public async Task A_settings_change_is_applied_straight_away()
    {
        var cut = Render<AppearanceSync>();

        await _settings.SaveAsync(_settings.Current with { Appearance = new AppearanceSettings { Density = Density.Compact } }, Xunit.TestContext.Current.CancellationToken);

        await cut.WaitForAssertionAsync(() => Theme.Current!.Appearance.ShouldBe(new GfoAppearance(GfoThemeMode.Dark, null, GfoDensity.Compact)));
    }

    [Fact]
    public async Task A_save_that_leaves_the_appearance_alone_does_not_re_apply()
    {
        Render<AppearanceSync>();

        await _settings.SaveAsync(_settings.Current with { Window = new WindowPlacement(0, 0, 1440, 900, 96, Maximized: false) }, Xunit.TestContext.Current.CancellationToken);

        Fluent.Created.Count.ShouldBe(1);
    }

    [Fact]
    public async Task After_the_page_goes_away_changes_are_ignored()
    {
        var cut = Render<AppearanceSync>();
        cut.Instance.Dispose();

        await _settings.SaveAsync(_settings.Current with { Appearance = new AppearanceSettings() }, Xunit.TestContext.Current.CancellationToken);

        Fluent.Created.Count.ShouldBe(1);
    }
}

public sealed class SettingsPageTests : GfoTestContext
{
    private readonly FakeSettingsStore _settings = new(new AppSettings { Developer = new DeveloperSettings { LogSql = true } });

    public SettingsPageTests()
    {
        Services.AddSingleton<ISettingsStore>(_settings);
        Services.AddSingleton<GridironFrontOffice.Application.Saves.ISaveManager>(new TestDoubles.FakeSaveManager(new TestDoubles.FakeDomainEvents()));
        Services.AddSingleton(TestDoubles.SaveScreenContext.App);
    }

    [Fact]
    public void Each_setting_shows_its_saved_choice()
    {
        var cut = Render<SettingsPage>();

        Checked(cut, "theme").ShouldBe("Dark");
        Checked(cut, "accent").ShouldBe("Team");
        Checked(cut, "density").ShouldBe("Comfortable");
    }

    [Theory]
    [InlineData("theme", "Light")]
    [InlineData("accent", "Neutral")]
    [InlineData("density", "Compact")]
    public void Choosing_an_option_saves_it_and_keeps_every_other_setting(string group, string option)
    {
        var cut = Render<SettingsPage>();

        cut.Find($"[data-testid={group}]").QuerySelectorAll("[role=radio]").Single(o => o.TextContent.Trim() == option).Click();

        var saved = _settings.Saved.ShouldHaveSingleItem();
        (group switch
        {
            "theme" => saved.Appearance.Theme.ToString(),
            "accent" => saved.Appearance.Accent.ToString(),
            _ => saved.Appearance.Density.ToString(),
        }).ShouldBe(option);
        saved.Developer.LogSql.ShouldBeTrue("Saving appearance never resets other settings.");
        Checked(cut, group).ShouldBe(option);
    }

    private static string Checked(IRenderedComponent<SettingsPage> cut, string group) =>
        cut.Find($"[data-testid={group}] [role=radio][aria-checked=true]").TextContent.Trim();
}

/// <summary>settings.json in memory: saves become current and raise Changed, as the real store does.</summary>
internal sealed class FakeSettingsStore(AppSettings initial) : ISettingsStore
{
    public AppSettings Current { get; private set; } = initial;

    public List<AppSettings> Saved { get; } = [];

    public event EventHandler<AppSettings>? Changed;

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Saved.Add(settings);
        Current = settings;
        Changed?.Invoke(this, settings);
        return Task.CompletedTask;
    }
}
