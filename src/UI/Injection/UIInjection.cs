using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Injection;

public static class UIInjection
{
	public static void Configure(IServiceCollection services)
	{
		services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<ILeagueWizardService, LeagueSetupService>();
	}
}
