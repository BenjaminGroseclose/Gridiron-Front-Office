using GridironFrontOffice.Application;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Infrastructure;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.UI.Components.Overlays;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using GridironFrontOffice.UI.Tests.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.TestDoubles;

/// <summary>
/// A bUnit context over the real app (D-093 #1): the hosts' Infrastructure, Application and UI services in a temp
/// folder, so a screen saves, reads and picks as it does in the app. Only what a build must not touch is fake: the
/// platform (the Recycle Bin, quitting), dialogs, toasts, settings.json, and the clock, which reads 2031 so "this
/// year" can't pass by chance.
/// </summary>
public abstract class RealScreenContext : GfoTestContext
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gfo-tests", Guid.NewGuid().ToString("N"));

    protected RealScreenContext()
    {
        Directory.CreateDirectory(_root);
        Services.AddSingleton<TimeProvider>(Clock);   // before Infrastructure, which only adds the system clock if none is there
        Services.AddSingleton<IPlatformServices>(Platform);
        Services.AddGfoInfrastructure(AppPaths.ForRoot(_root), SaveScreenContext.App);
        Services.AddGfoApplication();
        Services.AddGfoUi();
        Services.AddSingleton<IGfoDialogs>(Dialogs);
        Services.AddSingleton<IGfoToasts>(Toasts);
        Services.AddSingleton<ISettingsStore>(Settings);
    }

    /// <summary>The year the clock reads: not the real one, so a page that ignores the clock fails.</summary>
    internal const int ThisYear = 2031;

    internal FixedClock Clock { get; } = new(new DateTimeOffset(ThisYear, 3, 14, 12, 0, 0, TimeSpan.Zero));

    internal FakePlatformServices Platform { get; } = new();

    internal FakeDialogs Dialogs { get; } = new();

    internal FakeToasts Toasts { get; } = new();

    internal FakeSettingsStore Settings { get; } = new(new AppSettings());

    internal ISaveManager Saves => Services.GetRequiredService<ISaveManager>();

    internal LeagueQueries Queries => Services.GetRequiredService<LeagueQueries>();

    internal ISeedData Data => Services.GetRequiredService<ISeedData>();

    /// <summary>Where the app is now, relative to the base: "", "pick-team", "finalize?team=7"…</summary>
    protected string Location
    {
        get
        {
            var navigation = Services.GetRequiredService<NavigationManager>();
            return navigation.ToBaseRelativePath(navigation.Uri);
        }
    }

    /// <summary>
    /// The bound on every wait for a page's read, which runs on the thread pool: long enough for a loaded build,
    /// short enough that a page that never gets there fails instead of hanging the run.
    /// </summary>
    protected static TimeSpan Bound { get; } = TimeSpan.FromSeconds(10);

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected void GoTo(string uri) => Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    /// <summary>
    /// Writes <paramref name="league"/> to a new save named "Seed 42" and opens it (SaveOpened), as New League does
    /// once it has generated: the seed-42 league is generated once per class (<see cref="State.GeneratedSeed42"/>).
    /// </summary>
    internal async Task<OpenSave> OpenLeagueAsync(GeneratedLeague league)
    {
        var created = await Saves.CreateAsync("Seed 42", (uow, ct) => LeagueWriter.WriteAsync(uow, league, Data, ct),
            TestContext.Current.CancellationToken);
        created.IsSuccess.ShouldBeTrue(created.ToString());
        return created.Value;
    }

    /// <summary>Picks the user's team through the app's own command (UserTeamPicked).</summary>
    internal async Task PickAsync(int team)
    {
        var picked = await Services.GetRequiredService<LeagueCommands>()
            .PickUserTeamAsync(new TeamID(team), TestContext.Current.CancellationToken);
        picked.IsSuccess.ShouldBeTrue(picked.ToString());
    }

    /// <summary>The open league's header, read fresh.</summary>
    internal Task<LeagueHeaderDto> HeaderAsync() => Queries.GetHeaderAsync(TestContext.Current.CancellationToken);

    /// <summary>The .gfo files in the saves folder.</summary>
    internal IReadOnlyList<string> SaveFiles()
    {
        var saves = Services.GetRequiredService<IAppPaths>().Saves;
        return Directory.Exists(saves) ? Directory.GetFiles(saves, "*.gfo") : [];
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await Saves.CloseAsync();   // releases the save's file before the folder goes
        await base.DisposeAsyncCore();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A file still held by a just-disposed service is cleaned up with %TEMP% later.
        }
    }
}

/// <summary>A clock stopped at one instant, in UTC.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
