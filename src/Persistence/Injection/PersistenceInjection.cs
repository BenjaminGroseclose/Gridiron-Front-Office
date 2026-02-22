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
		services.AddScoped<IBaseRepository<League>, BaseRepository<League>>();
		services.AddScoped<IBaseRepository<Player>, BaseRepository<Player>>();
		services.AddScoped<IBaseRepository<Stadium>, BaseRepository<Stadium>>();
		services.AddScoped<IBaseRepository<Conference>, BaseRepository<Conference>>();
		services.AddScoped<IBaseRepository<Division>, BaseRepository<Division>>();
		services.AddScoped<IBaseRepository<Team>, BaseRepository<Team>>();
		services.AddScoped<IBaseRepository<Game>, BaseRepository<Game>>();
		services.AddScoped<IBaseRepository<Season>, BaseRepository<Season>>();
		services.AddScoped<IBaseRepository<TeamSeason>, BaseRepository<TeamSeason>>();
	}
}