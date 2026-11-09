using GridironFrontOffice.Application;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Infrastructure;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.UI.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.UI.Tests.TestDoubles;

/// <summary>
/// The real app behind a UI test (D-093 #1): the hosts' Infrastructure and Application services over a temp folder,
/// with nothing open until the test opens it. The reader is wrapped in a <see cref="HeaderGate"/>, so a test can hold
/// a header load open.
/// </summary>
internal sealed class RealApp : IAsyncDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "gfo-tests", Guid.NewGuid().ToString("N"));

	public RealApp()
	{
		Directory.CreateDirectory(_root);
		var services = new ServiceCollection()
			.AddLogging(logging => logging.AddProvider(Logs))
			.AddSingleton<IPlatformServices>(new FakePlatformServices())
			.AddGfoInfrastructure(AppPaths.ForRoot(_root), AppIdentity.Parse("0.0.1+test", "Test"))
			.AddGfoApplication()
			.AddGfoUi();

		// The app's reader, behind the gate: SaveReader is Infrastructure's own, so it's built from its registration
		var reader = services.Single(s => s.ServiceType == typeof(ISaveReader));
		services.Replace(ServiceDescriptor.Singleton<ISaveReader>(sp =>
			new HeaderGate((ISaveReader)ActivatorUtilities.CreateInstance(sp, reader.ImplementationType!))));
		Services = services.BuildServiceProvider();
	}

	public ServiceProvider Services { get; }

	public ListLoggerProvider Logs { get; } = new();

	public ISaveManager Saves => Services.GetRequiredService<ISaveManager>();

	public IDomainEventDispatcher Events => Services.GetRequiredService<IDomainEventDispatcher>();

	public LeagueQueries Queries => Services.GetRequiredService<LeagueQueries>();

	public ISaveWriter Writer => Services.GetRequiredService<ISaveWriter>();

	public HeaderGate Gate => (HeaderGate)Services.GetRequiredService<ISaveReader>();

	public ISeedData Data => Services.GetRequiredService<ISeedData>();

	/// <summary>A store as AddGfoUi would build it.</summary>
	public GameStateStore NewStore() =>
		new(Saves, Events, Queries, Services.GetRequiredService<ILogger<GameStateStore>>());

	/// <summary>Writes <paramref name="league"/> to a new save and opens it, as New League does (SaveOpened).</summary>
	public async Task<OpenSave> CreateLeagueAsync(GeneratedLeague league)
	{
		var created = await Saves.CreateAsync("Seed 42", (uow, ct) => LeagueWriter.WriteAsync(uow, league, Data, ct),
			TestContext.Current.CancellationToken);
		created.IsSuccess.ShouldBeTrue(created.ToString());
		return created.Value;
	}

	/// <summary>Generates the seed-42 league for 2026 and opens it, as New League does: about half a second.</summary>
	public async Task<OpenSave> GenerateLeagueAsync()
	{
		var created = await Services.GetRequiredService<CreateLeague>().RunAsync(new NewLeague("Seed 42", 42, 2026),
			cancellationToken: TestContext.Current.CancellationToken);
		created.IsSuccess.ShouldBeTrue(created.ToString());
		return created.Value;
	}

	/// <summary>Picks the user's team, as the wizard's summary does (UserTeamPicked).</summary>
	public async Task PickAsync(int team)
	{
		var picked = await new LeagueCommands(Writer, NullLogger<LeagueCommands>.Instance)
			.PickUserTeamAsync(new TeamID(team), TestContext.Current.CancellationToken);
		picked.IsSuccess.ShouldBeTrue(picked.ToString());
	}

	public async ValueTask DisposeAsync()
	{
		Gate.Release();             // a test that failed while a read was held doesn't leave it waiting
		await Saves.CloseAsync();   // releases the save's file (SqliteFile.Release)
		await Services.DisposeAsync();
		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// A file still held by a just-disposed logger is cleaned up with %TEMP% later.
		}
	}
}

/// <summary>
/// The app's reader, which can hold the next header read after it has run: the read sees the save as it was, and its
/// result reaches the caller only when the test releases it. Every other read passes straight through.
/// </summary>
internal sealed class HeaderGate(ISaveReader inner) : ISaveReader
{
	private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private TaskCompletionSource _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private int _armed;

	/// <summary>
	/// Waits, at most 10 s, until the held header read has run and waits for <see cref="Release"/>: a read that never
	/// comes fails the test instead of hanging it.
	/// </summary>
	public async Task HeldAsync(CancellationToken cancellationToken)
	{
#pragma warning disable VSTHRD003 // The gate's own signal, which QueryAsync sets; nothing here needs a synchronization context.
		await _held.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
#pragma warning restore VSTHRD003
	}

	/// <summary>Holds the next header read once it has run.</summary>
	public void HoldNext()
	{
		_release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Volatile.Write(ref _armed, 1);
	}

	public void Release() => _release.TrySetResult();

	public async Task<T> QueryAsync<T>(Func<ILeagueReadDb, CancellationToken, Task<T>> query, CancellationToken cancellationToken = default)
	{
		var result = await inner.QueryAsync(query, cancellationToken);
		if (typeof(T) == typeof(LeagueHeaderDto) && Interlocked.Exchange(ref _armed, 0) == 1)
		{
			_held.TrySetResult();
#pragma warning disable VSTHRD003 // The gate's own signal, which the test sets; nothing here needs a synchronization context.
			await _release.Task;
#pragma warning restore VSTHRD003
		}

		return result;
	}
}
