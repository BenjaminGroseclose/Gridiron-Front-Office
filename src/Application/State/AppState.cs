using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application.State;

/// <summary>
/// The base application state, holding minimal information about the current session.
/// </summary>
public class AppState
{
	private readonly object _sync = new();

	public record StateSnapshot(
		int? UserTeamID,
		Season? CurrentSeason,
		string? CurrentSavePath,
		string? CurrentRoute,
		Stack<string> RouteHistory,
		bool IsLoading,
		string? Error,
		DateTime? CurrentDateTime
	);

	public StateSnapshot CurrentState { get; private set; } = new StateSnapshot(
		UserTeamID: null,
		CurrentSeason: null,
		CurrentSavePath: null,
		CurrentRoute: null,
		RouteHistory: new Stack<string>(),
		IsLoading: false,
		Error: null,
		CurrentDateTime: null
	);

	public event Action<StateSnapshot>? OnStateChanged;

	/// <summary>
	/// Updates the application state and notifies subscribers.
	/// Subscribers receive a snapshot of the new state.
	/// </summary>
	public void Subscribe(Action<StateSnapshot> listener)
	{
		OnStateChanged += listener;
	}

	/// <summary>
	/// Unsubscribes a listener from state change notifications.
	/// </summary>
	public void Unsubscribe(Action<StateSnapshot> listener)
	{
		OnStateChanged -= listener;
	}

	/// <summary>
	/// Updates the current application state using the provided update function.
	/// </summary>
	/// <param name="updateFunc">The function that takes the current state and returns the updated state.</param>
	public void UpdateState(Func<StateSnapshot, StateSnapshot> updateFunc)
	{
		lock (_sync)
		{
			CurrentState = updateFunc(CurrentState);
		}
		OnStateChanged?.Invoke(CurrentState);
	}

	public void PushRoute(string route)
	{
		UpdateState(s =>
		{
			var newHistory = new Stack<string>(s.RouteHistory.Reverse());
			if (!string.IsNullOrEmpty(s.CurrentRoute))
			{
				newHistory.Push(s.CurrentRoute);
			}
			return s with { CurrentRoute = route, RouteHistory = new Stack<string>(newHistory.Reverse()) };
		});
	}

	public void PopRoute()
	{
		UpdateState(s =>
		{
			var newHistory = new Stack<string>(s.RouteHistory.Reverse());
			string? previousRoute = null;
			if (newHistory.Count > 0)
			{
				previousRoute = newHistory.Pop();
			}
			return s with { CurrentRoute = previousRoute, RouteHistory = new Stack<string>(newHistory.Reverse()) };
		});
	}

	// Convenience methods for common state updates can be added here
	public void SetSavePath(string? savePath) =>
		UpdateState(s => s with { CurrentSavePath = savePath });
	public void SetSeason(Season? season) =>
		UpdateState(s => s with { CurrentSeason = season });
	public void SetLoading(bool IsLoading) =>
		UpdateState(s => s with { IsLoading = IsLoading });
	public void SetError(string? error) =>
		UpdateState(s => s with { Error = error });
	public void SetCurrentTime(DateTime? currentTime) =>
		UpdateState(s => s with { CurrentDateTime = currentTime });
}