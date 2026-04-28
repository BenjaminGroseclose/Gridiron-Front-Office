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
			_appState.PopRoute();

			var route = _appState.CurrentState.CurrentRoute;
			if (string.IsNullOrWhiteSpace(route))
			{
				_logger.LogWarning("No valid route found after popping route history.");
				return;
			}

			_navigationManager.NavigateTo(route);
		}
		else
		{
			_logger.LogWarning("No previous route to navigate back to.");
		}
	}

	public void NavigateTo(string route, Dictionary<string, object>? queryParameters = null)
	{
		_logger.LogInformation("Navigating to {Route}", route);

		_appState.PushRoute(route);

		// Construct the full URL with query parameters if provided
		string fullUrl = route;
		if (queryParameters != null && queryParameters.Count > 0)
		{
			fullUrl = _navigationManager.GetUriWithQueryParameters(route, queryParameters);
		}

		// Perform the actual navigation
		_navigationManager.NavigateTo(fullUrl);
	}
}
