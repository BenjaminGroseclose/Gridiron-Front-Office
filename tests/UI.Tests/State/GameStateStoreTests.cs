using System.Runtime.CompilerServices;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.UI.State;
using GridironFrontOffice.UI.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.UI.Tests.State;

/// <summary>
/// M1 WP6 slice 3 (guide §5): GameStateStore v0 on a real save (D-093 #1). Each test writes the seed-42 league,
/// generated once, to a save of its own.
/// </summary>
public sealed class GameStateStoreTests(GeneratedSeed42 seed) : IClassFixture<GeneratedSeed42>
{
	private static readonly LeagueHeaderDto AtCamp = new("Seed 42", 2026, Phase.TrainingCamp, LeagueCalendar.TrainingCampOpens(2026), null);

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task At_the_main_menu_there_is_no_header_and_nothing_loads()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();

		store.Header.ShouldBeNull();
		store.Loading.IsCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task Opening_a_league_loads_its_header()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);

		await app.CreateLeagueAsync(seed.League);
		await Settled(store.Loading);

		store.Header.ShouldBe(AtCamp);
		changes.Value.ShouldBe(1);
	}

	[Fact]
	public async Task A_pick_reloads_it_with_the_user_teams_summary()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);
		await app.CreateLeagueAsync(seed.League);
		await Settled(store.Loading);

		await app.PickAsync(5);
		await Settled(store.Loading);

		store.Header.ShouldBe(await app.Queries.GetHeaderAsync(Ct));
		store.Header.ShouldNotBeNull().UserTeam.ShouldNotBeNull().Identity.TeamID.ShouldBe(new TeamID(5));
		changes.Value.ShouldBe(2);
	}

	[Fact]
	public async Task Closing_clears_it()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);
		await app.CreateLeagueAsync(seed.League);
		await Settled(store.Loading);

		await app.Saves.CloseAsync(Ct);

		store.Header.ShouldBeNull();
		changes.Value.ShouldBe(2);
	}

	[Fact]
	public async Task Restoring_the_created_snapshot_brings_back_no_user_team()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var save = await app.CreateLeagueAsync(seed.League);
		await app.PickAsync(5);
		await Settled(store.Loading);
		store.Header.ShouldNotBeNull().UserTeam.ShouldNotBeNull();

		// A restore reopens the save (SaveOpened), so the store reloads
		var created = (await app.Services.GetRequiredService<ISnapshotService>().ListAsync(save.SaveID, Ct)).Single(s => s.Kind == SnapshotKind.Created);
		(await app.Saves.RestoreAsync(created, Ct)).IsSuccess.ShouldBeTrue();
		await Settled(store.Loading);

		store.Header.ShouldBe(AtCamp);
	}

	[Fact]
	public async Task A_store_built_while_a_league_is_open_loads_it()
	{
		await using var app = new RealApp();
		await app.CreateLeagueAsync(seed.League);
		await app.PickAsync(5);

		using var store = app.NewStore();
		await Settled(store.Loading);

		store.Header.ShouldBe(await app.Queries.GetHeaderAsync(Ct));
	}

	[Fact]
	public async Task A_load_that_finishes_after_a_later_one_is_dropped()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);

		// The open's load reads "no user team", then waits until after the pick's load has finished
		app.Gate.HoldNext();
		await app.CreateLeagueAsync(seed.League);
		var opened = store.Loading;
		await app.Gate.HeldAsync(Ct);
		await app.PickAsync(5);
		await Settled(store.Loading);
		app.Gate.Release();
		await Settled(opened);

		store.Header.ShouldNotBeNull().UserTeam.ShouldNotBeNull("The pick's load is the latest, so the open's result is dropped.");
		changes.Value.ShouldBe(1);
	}

	[Fact]
	public async Task A_load_that_finishes_after_a_close_is_dropped()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);

		app.Gate.HoldNext();
		await app.CreateLeagueAsync(seed.League);
		var opened = store.Loading;
		await app.Gate.HeldAsync(Ct);
		await app.Saves.CloseAsync(Ct);
		app.Gate.Release();
		await Settled(opened);

		store.Header.ShouldBeNull("Back at the main menu, a header read before the close stays out.");
		changes.Value.ShouldBe(0);
	}

	[Fact]
	public async Task A_failed_load_logs_8004_and_keeps_the_header()
	{
		await using var app = new RealApp();
		using var store = app.NewStore();
		var changes = Count(store);
		await app.CreateLeagueAsync(seed.League);
		await Settled(store.Loading);

		// The user team's summary reads the season's cap: without the row, the pick's load throws
		await app.Writer.ExecuteAsync("Test.DropTheSeason", async (uow, ct) =>
		{
			await using var delete = uow.CreateCommand("DELETE FROM Seasons;");
			await delete.ExecuteNonQueryAsync(ct);
			return Result.Ok();
		}, Ct);
		await app.PickAsync(5);
		await Settled(store.Loading);

		store.Header.ShouldBe(AtCamp);
		changes.Value.ShouldBe(1);
		var logged = app.Logs.Entries.Where(e => e.EventID == 8004).ShouldHaveSingleItem();
		(logged.Category, logged.Level, logged.Message)
			.ShouldBe((typeof(GameStateStore).FullName!, LogLevel.Error, "Couldn't load the league header after UserTeamPicked"));
		logged.Exception.ShouldBeOfType<InvalidOperationException>();
		store.Loading.IsCompletedSuccessfully.ShouldBeTrue("A failed load is logged, never thrown.");
	}

	[Fact]
	public async Task Disposing_the_store_unsubscribes_it()
	{
		await using var app = new RealApp();
		var events = new FakeDomainEvents();
		var store = new GameStateStore(app.Saves, events, app.Queries, NullLogger<GameStateStore>.Instance);
		events.Subscribers.ShouldBe(3, "SaveOpened, UserTeamPicked and SaveClosed.");

		store.Dispose();

		events.Subscribers.ShouldBe(0);
	}

	[Fact]
	public async Task AddGfoUi_registers_one_store_per_scope()
	{
		await using var app = new RealApp();
		GameStateStore first;
		await using (var scope = app.Services.CreateAsyncScope())
		{
			first = scope.ServiceProvider.GetRequiredService<GameStateStore>();
			scope.ServiceProvider.GetRequiredService<GameStateStore>().ShouldBeSameAs(first);
		}

		await using var other = app.Services.CreateAsyncScope();
		other.ServiceProvider.GetRequiredService<GameStateStore>().ShouldNotBeSameAs(first);
	}

	/// <summary>
	/// Waits for <paramref name="task"/>, at most 10 s: a store that leaves a load waiting on the gate (or never starts
	/// the one a test expects) fails with a TimeoutException instead of hanging the run.
	/// </summary>
	private static Task Settled(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), Ct);

	/// <summary>How many times <paramref name="store"/> raised Changed.</summary>
	private static StrongBox<int> Count(GameStateStore store)
	{
		var count = new StrongBox<int>();
		store.Changed += () => Interlocked.Increment(ref count.Value);
		return count;
	}
}

/// <summary>The seed-42 league for 2026, generated once for the store's tests.</summary>
public sealed class GeneratedSeed42 : IAsyncLifetime
{
	internal GeneratedLeague League { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		await using var app = new RealApp();
		League = new LeagueGenerator(app.Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System).Generate(42, 2026);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
