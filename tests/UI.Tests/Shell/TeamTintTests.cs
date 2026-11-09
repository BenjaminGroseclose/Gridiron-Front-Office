using Bunit;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.UI.Components.Theming;
using GridironFrontOffice.UI.Shell;
using GridironFrontOffice.UI.State;
using GridironFrontOffice.UI.Tests.State;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.Shell;

/// <summary>
/// M1 WP8 slice 2 (guide §4): the team tint on a real seed-42 save. AppearanceSync follows GameStateStore, so the
/// accent is neutral until the pick, the user team's primary from then on, and neutral again once the league closes.
/// </summary>
public sealed class TeamTintTests(GeneratedSeed42 seed) : RealScreenContext, IClassFixture<GeneratedSeed42>
{
    [Theory]
    [InlineData(ThemeChoice.Dark, GfoThemeMode.Dark)]
    [InlineData(ThemeChoice.Light, GfoThemeMode.Light)]
    public async Task The_accent_is_neutral_until_the_pick_then_the_teams_primary(ThemeChoice choice, GfoThemeMode mode)
    {
        await Settings.SaveAsync(Settings.Current with { Appearance = new AppearanceSettings { Theme = choice } }, Ct);
        var cut = Render<AppearanceSync>();

        await OpenLeagueAsync(seed.League);
        await SettledAsync(cut);
        Theme.Current!.Appearance.ShouldBe(new GfoAppearance(mode, null, GfoDensity.Comfortable), "A league without a team is neutral.");

        await PickAsync(5);

        await SettledAsync(cut);
        var primary = (await HeaderAsync()).UserTeam.ShouldNotBeNull().Identity.PrimaryColor;
        Theme.Current!.Appearance.ShouldBe(new GfoAppearance(mode, primary, GfoDensity.Comfortable));
    }

    [Fact]
    public async Task Closing_the_league_goes_back_to_neutral()
    {
        var cut = Render<AppearanceSync>();
        await OpenLeagueAsync(seed.League);
        await PickAsync(5);
        await SettledAsync(cut);
        Theme.Current!.Appearance.AccentColor.ShouldNotBeNull();

        await Saves.CloseAsync(Ct);
        await SettledAsync(cut);

        Theme.Current!.Appearance.AccentColor.ShouldBeNull();
    }

    [Fact]
    public async Task Accent_Neutral_stays_neutral_after_the_pick()
    {
        await Settings.SaveAsync(Settings.Current with { Appearance = new AppearanceSettings { Accent = AccentMode.Neutral } }, Ct);
        var cut = Render<AppearanceSync>();
        await OpenLeagueAsync(seed.League);

        await PickAsync(5);
        await SettledAsync(cut);

        Services.GetRequiredService<GameStateStore>().Header!.UserTeam.ShouldNotBeNull("The store has the pick.");
        Theme.Current!.Appearance.AccentColor.ShouldBeNull();
    }

    // The store's latest load, then the re-apply it queued on the renderer. AppearanceSync renders nothing, so bUnit's
    // WaitFor never looks again: a wait has to be on the store, and the theme is read once both are done.
    private static async Task SettledAsync(IRenderedComponent<AppearanceSync> cut)
    {
        var store = cut.Services.GetRequiredService<GameStateStore>();
#pragma warning disable VSTHRD003 // The store's own load, which it starts and completes; nothing here needs a synchronization context.
        await store.Loading.WaitAsync(Bound, Ct);
#pragma warning restore VSTHRD003
        await cut.InvokeAsync(() => { });
    }
}
