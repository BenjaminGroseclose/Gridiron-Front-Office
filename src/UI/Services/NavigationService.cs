using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Application.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.UI.Services;

public class NavigationService : INavigationService
{
	private readonly NavigationManager _navigationManager;
	private readonly AppState _appState;
	private readonly ILogger<NavigationService> _logger;

	public NavigationService(NavigationManager navigationManager, AppState appState, ILogger<NavigationService> logger)
	{
		_navigationManager = navigationManager;
		_appState = appState;
		_logger = logger;
	}

	public bool CanNavigateBack => _appState.CurrentState.RouteHistory.Count > 0;

	public void NavigateBack()
	{
		_logger.LogInformation("Navigating back from {CurrentRoute} to previous route", _appState.CurrentState.CurrentRoute);

		if (this.CanNavigateBack)
		{
			// Track the route in AppState
			_appState.PopRoute();

			// Perform the actual navigation
			_navigationManager.NavigateTo(_appState.CurrentState.CurrentRoute);
		}
		else
		{
			_logger.LogWarning("No previous route to navigate back to.");
		}
	}

	public void NavigateTo(string route)
	{
		_logger.LogInformation("Navigating to {Route}", route);

		// Track the route in AppState
		_appState.PushRoute(route);

		// Perform the actual navigation
		_navigationManager.NavigateTo(route);
	}
}
