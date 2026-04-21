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

		// SeedDataService is stateless JSON loading — singleton is safe and avoids repeated file reads
		services.AddSingleton<ISeedDataService, SeedDataService>();

		// Register repositories here
		services.AddScoped<IBaseRepository<LeagueSetting>, BaseRepository<LeagueSetting>>();
		services.AddScoped<IBaseRepository<Player>, BaseRepository<Player>>();
		services.AddScoped<IBaseRepository<Stadium>, BaseRepository<Stadium>>();
		services.AddScoped<IBaseRepository<Team>, BaseRepository<Team>>();
		services.AddScoped<IBaseRepository<Game>, BaseRepository<Game>>();
		services.AddScoped<IBaseRepository<Season>, BaseRepository<Season>>();
		services.AddScoped<IBaseRepository<Week>, BaseRepository<Week>>();
		services.AddScoped<IBaseRepository<TeamSeason>, BaseRepository<TeamSeason>>();
		services.AddScoped<IBaseRepository<Contract>, BaseRepository<Contract>>();
		services.AddScoped<IBaseRepository<ContractYear>, BaseRepository<ContractYear>>();
		services.AddScoped<IBaseRepository<DraftPick>, BaseRepository<DraftPick>>();
	}
}