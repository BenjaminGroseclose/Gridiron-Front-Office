using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Application;

/// <summary>ARCHITECTURE §6.1: the Application services both hosts register, after AddGfoInfrastructure and before AddGfoUi.</summary>
public static class ApplicationServices
{
	/// <summary>
	/// Registers perception and the query services (M1 WP6 §2). They hold no state and read through ISaveReader, so they're
	/// singletons. CreateLeague and LeagueCommands join in WP8, with their first consumer.
	/// </summary>
	public static IServiceCollection AddGfoApplication(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddSingleton<IPerception, Perception>();
		services.AddSingleton<LeagueQueries>();
		services.AddSingleton<PlayerQueries>();
		return services;
	}
}
