using Microsoft.Extensions.DependencyInjection;
using GridironFrontOffice.Application.State;
using GridironFrontOffice.Application.Interfaces;

namespace GridironFrontOffice.Application.Injection;

public static class ApplicationInjection
{
	public static void Configure(IServiceCollection services)
	{
		// Register minimal application state as a singleton
		services.AddSingleton<AppState>();

		services.AddScoped<IPlayerGeneratorService, PlayerGeneratorService>();
	}
}