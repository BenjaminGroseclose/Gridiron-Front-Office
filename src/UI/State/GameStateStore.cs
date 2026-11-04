using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.UI.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.UI.State;

/// <summary>
/// The open league's header for the shell and Home (UI_DESIGN_SYSTEM §9, M1 WP6 §4.3). Null at the main menu. It loads
/// when a save opens (and at construction if one is open), again when the user picks a team, and clears when the save
/// closes. Other events refresh it from M3 (D-078 #14).
/// </summary>
public sealed class GameStateStore : IDisposable
{
	private readonly LeagueQueries _queries;
	private readonly ILogger<GameStateStore> _log;
	private readonly IDisposable[] _subscriptions;
	private readonly Lock _gate = new();
	private LeagueHeaderDto? _header;
	private int _latest;   // numbers each load and clear: only the latest one's result is kept
	private bool _disposed;

	public GameStateStore(ISaveManager saves, IDomainEventDispatcher events, LeagueQueries queries, ILogger<GameStateStore> log)
	{
		ArgumentNullException.ThrowIfNull(saves);
		ArgumentNullException.ThrowIfNull(events);
		_queries = queries ?? throw new ArgumentNullException(nameof(queries));
		_log = log ?? throw new ArgumentNullException(nameof(log));

		_subscriptions =
		[
			events.Subscribe<SaveOpened>(_ => Load(nameof(SaveOpened))),   // a restore reopens, so it lands here too
			events.Subscribe<UserTeamPicked>(_ => Load(nameof(UserTeamPicked))),
			events.Subscribe<SaveClosed>(_ => Clear()),
		];

		if (saves.Current is not null)
		{
			Load("construction");
		}
	}

	/// <summary>The open league's header, or null at the main menu (and until the first load finishes).</summary>
	public LeagueHeaderDto? Header
	{
		get
		{
			lock (_gate)
			{
				return _header;
			}
		}
	}

	/// <summary>
	/// The latest load, already complete when none is running. It never faults: a failed load logs 8004 and leaves
	/// <see cref="Header"/> as it was.
	/// </summary>
	public Task Loading { get; private set; } = Task.CompletedTask;

	/// <summary>Raised after <see cref="Header"/> changes, on whichever thread loaded it: components marshal with InvokeAsync.</summary>
	public event Action? Changed;

	public void Dispose()
	{
		lock (_gate)
		{
			_disposed = true;
			_latest++;   // a load still running is dropped
		}

		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}
	}

	/// <summary>
	/// Starts a load and returns. Event handlers run on the thread that finished the command, and SQLite's async calls
	/// complete synchronously, so the read moves to the thread pool rather than holding that thread.
	/// </summary>
	private void Load(string trigger)
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}

			var load = ++_latest;
			Loading = Task.Run(() => LoadAsync(load, trigger));   // inside the gate, so Loading is always the latest load
		}
	}

	private async Task LoadAsync(int load, string trigger)
	{
		LeagueHeaderDto header;
		try
		{
			header = await _queries.GetHeaderAsync().ConfigureAwait(false);
		}
#pragma warning disable CA1031 // A failed read is logged and the header kept; the app carries on (LOGGING §5).
		catch (Exception ex)
#pragma warning restore CA1031
		{
			// A load overtaken by a later one, or by a close, failed for nothing: only the latest one's failure is news
			if (IsLatest(load))
			{
				UiLog.HeaderLoadFailed(_log, ex, trigger);
			}

			return;
		}

		Set(load, header);
	}

	private void Clear()
	{
		int load;
		lock (_gate)
		{
			load = ++_latest;
		}

		Set(load, null);
	}

	/// <summary>
	/// Keeps <paramref name="header"/> only if no later load or clear has started since, and raises Changed only when it
	/// differs (the read models are records, so an equal reload changes nothing).
	/// </summary>
	private void Set(int load, LeagueHeaderDto? header)
	{
		lock (_gate)
		{
			if (load != _latest || _disposed || Equals(_header, header))
			{
				return;
			}

			_header = header;
		}

		Changed?.Invoke();
	}

	private bool IsLatest(int load)
	{
		lock (_gate)
		{
			return load == _latest && !_disposed;
		}
	}
}
