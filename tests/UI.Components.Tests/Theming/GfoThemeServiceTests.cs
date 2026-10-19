using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using GridironFrontOffice.UI.Components.Theming;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Tests.Theming;

/// <summary>UI_DESIGN_SYSTEM.md §3.2–3.3 and D-067: what GfoThemeService hands Fluent, and what it writes to the page.</summary>
public sealed class GfoThemeServiceTests : GfoTestContext
{
    [Fact]
    public async Task The_default_is_Dark_with_the_exact_neutral_accent_and_never_System()
    {
        await Theme.ApplyAsync(new GfoAppearance());

        var settings = Fluent.Created.ShouldHaveSingleItem();
        (settings.Mode, settings.IsExact, settings.Color).ShouldBe((ThemeMode.Dark, true, GfoAppearance.NeutralAccent));
        Applied.Invocations.ShouldHaveSingleItem().Arguments.ShouldBe(["dark", "comfortable"]);
        Theme.Current!.IsDark.ShouldBeTrue();
    }

    [Fact]
    public async Task The_Stadium_palette_fonts_radii_and_accent_colors_are_set_on_the_theme()
    {
        await Theme.ApplyAsync(new GfoAppearance(GfoThemeMode.Light, "#1B2A4A"));

        var theme = Fluent.Applied.ShouldHaveSingleItem();
        var light = StadiumPalette.Light;
        (theme.Colors.Neutral.Background1, theme.Colors.Neutral.Background3, theme.Colors.Neutral.Foreground1)
            .ShouldBe((light.Surface1, light.Ground, light.Text));
        theme.Fonts.Family.Base.ShouldStartWith("'Archivo'");
        (theme.Borders.Radius.Small, theme.Borders.Radius.Medium, theme.Borders.Radius.Large, theme.Borders.Radius.Circular)
            .ShouldBe(("8px", "14px", "20px", "999px"));

        var state = Theme.Current!;
        theme.Colors.Neutral.ForegroundOnBrand.ShouldBe(state.OnAccent);
        theme.Colors.Brand.ForegroundLink.ShouldBe(state.AccentText);
        theme.Colors.Compound.BrandForeground1.ShouldBe(state.Accent);
        state.AccentFill.ShouldBe("#1B2A4A");
    }

    [Fact]
    public async Task Match_Windows_follows_the_Windows_setting_and_its_changes()
    {
        WindowsIs(dark: false);
        await Theme.ApplyAsync(new GfoAppearance(GfoThemeMode.MatchWindows));
        Fluent.Created[^1].Mode.ShouldBe(ThemeMode.Light);

        await Theme.OnWindowsThemeChangedAsync(dark: true);

        Fluent.Created[^1].Mode.ShouldBe(ThemeMode.Dark);
        Theme.Current!.IsDark.ShouldBeTrue();
        Fluent.Created.ShouldAllBe(s => s.Mode != ThemeMode.System);
    }

    [Fact]
    public async Task An_explicit_mode_ignores_Windows_but_still_re_applies_on_a_switch()
    {
        await Theme.ApplyAsync(new GfoAppearance(GfoThemeMode.Dark));

        await Theme.OnWindowsThemeChangedAsync(dark: false);

        Fluent.Created.Count.ShouldBe(2, "The re-apply is the safety net in case Fluent's own listener comes back.");
        Fluent.Created[^1].Mode.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task Fluents_cached_settings_are_cleared_once()
    {
        await Theme.ApplyAsync(new GfoAppearance());
        await Theme.ApplyAsync(new GfoAppearance(GfoThemeMode.Light));

        Fluent.CacheClears.ShouldBe(1);
    }

    [Fact]
    public async Task Density_reaches_the_page_and_the_row_height()
    {
        var applied = new List<GfoThemeState>();
        Theme.Applied += (_, state) => applied.Add(state);

        await Theme.ApplyAsync(new GfoAppearance(Density: GfoDensity.Compact));

        Applied.Invocations.Last().Arguments.ShouldBe(["dark", "compact"]);
        applied.ShouldHaveSingleItem().RowHeight.ShouldBe(36);
        (Theme.Current! with { Appearance = new GfoAppearance() }).RowHeight.ShouldBe(46);
    }

    [Fact]
    public async Task A_change_from_Windows_before_the_first_apply_does_nothing()
    {
        await Theme.OnWindowsThemeChangedAsync(dark: false);

        Fluent.Created.ShouldBeEmpty();
        Theme.Current.ShouldBeNull();
    }
}
