using GridironFrontOffice.Domain;
using GridironFrontOffice.Persistence.Interfaces;
using GridironFrontOffice.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Persistence.Injection;

public static class PersistenceInjection
{
	public static void Configure(IServiceCollection services)
	{
		services.AddSingleton<GameManager>();
		services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();

		// Register seed data service
		services.AddScoped<ISeedDataService, SeedDataService>();

		// Register repositories here
		services.AddScoped<IBaseRepository<League>, LeagueRepository>();
	}
}