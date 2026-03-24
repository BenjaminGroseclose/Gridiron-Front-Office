using GridironFrontOffice.Domain;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Persistence;

public class GridironFrontOfficeDbContext : DbContext
{
	public GridironFrontOfficeDbContext(DbContextOptions<GridironFrontOfficeDbContext> options) : base(options)
	{
	}

	public DbSet<League> Leagues => Set<League>();
	public DbSet<Player> Players => Set<Player>();
	public DbSet<Stadium> Stadiums => Set<Stadium>();
	public DbSet<Conference> Conferences => Set<Conference>();
	public DbSet<Division> Divisions => Set<Division>();
	public DbSet<Team> Teams => Set<Team>();
	public DbSet<Game> Games => Set<Game>();
	public DbSet<Season> Seasons => Set<Season>();
	public DbSet<TeamSeason> TeamSeasons => Set<TeamSeason>();
	public DbSet<Week> Weeks => Set<Week>();
	public DbSet<GameStats> GameStats => Set<GameStats>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<League>().HasKey(x => x.LeagueID);
		modelBuilder.Entity<League>().Property(x => x.LeagueID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Player>().HasKey(x => x.PlayerID);
		modelBuilder.Entity<Player>().Property(x => x.PlayerID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Player>()
			.HasMany<Contract>()
			.WithOne(x => x.Player)
			.HasForeignKey(x => x.PlayerID);

		modelBuilder.Entity<Stadium>().HasKey(x => x.StadiumID);
		modelBuilder.Entity<Stadium>().Property(x => x.StadiumID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Conference>().HasKey(x => x.ConferenceID);
		modelBuilder.Entity<Conference>().Property(x => x.ConferenceID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Division>().HasKey(x => x.DivisionID);
		modelBuilder.Entity<Division>().Property(x => x.DivisionID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Division>()
			.HasOne<Conference>()
			.WithMany()
			.HasForeignKey(x => x.ConferenceID);

		modelBuilder.Entity<Team>().HasKey(x => x.TeamID);
		modelBuilder.Entity<Team>().Property(x => x.TeamID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Team>()
			.HasOne(x => x.Conference)
			.WithMany()
			.HasForeignKey(x => x.ConferenceID);
		modelBuilder.Entity<Team>()
			.HasOne(x => x.Division)
			.WithMany()
			.HasForeignKey(x => x.DivisionID);
		modelBuilder.Entity<Team>()
			.HasOne(x => x.Stadium)
			.WithMany()
			.HasForeignKey(x => x.StadiumID);

		modelBuilder.Entity<Season>().HasKey(x => x.SeasonID);
		modelBuilder.Entity<Season>().Property(x => x.SeasonID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Week>().HasKey(x => x.WeekID);
		modelBuilder.Entity<Week>().Property(x => x.WeekID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Week>()
			.HasOne<Season>()
			.WithMany()
			.HasForeignKey(x => x.SeasonID);

		modelBuilder.Entity<Game>().HasKey(x => x.GameID);
		modelBuilder.Entity<Game>().Property(x => x.GameID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Game>()
			.HasOne(x => x.Week)
			.WithMany(x => x.Games)
			.HasForeignKey(x => x.WeekID);
		modelBuilder.Entity<Game>()
			.HasOne<Season>()
			.WithMany()
			.HasForeignKey(x => x.SeasonID);

		modelBuilder.Entity<TeamSeason>().HasKey(x => x.TeamSeasonID);
		modelBuilder.Entity<TeamSeason>().Property(x => x.TeamSeasonID).ValueGeneratedOnAdd();
		modelBuilder.Entity<TeamSeason>()
			.HasOne<Team>()
			.WithMany()
			.HasForeignKey(x => x.TeamID);
		modelBuilder.Entity<TeamSeason>()
			.HasOne<Season>()
			.WithMany()
			.HasForeignKey(x => x.SeasonID);

		modelBuilder.Entity<GameStats>().HasKey(x => x.GameStatsID);
		modelBuilder.Entity<GameStats>().Property(x => x.GameStatsID).ValueGeneratedOnAdd();
		modelBuilder.Entity<GameStats>()
			.HasOne(x => x.Game)
			.WithMany()
			.HasForeignKey(x => x.GameID);
		modelBuilder.Entity<GameStats>()
			.HasOne(x => x.Team)
			.WithMany()
			.HasForeignKey(x => x.TeamID);

		modelBuilder.Entity<Contract>().HasKey(x => x.ContractID);
		modelBuilder.Entity<Contract>().Property(x => x.ContractID).ValueGeneratedOnAdd();

		modelBuilder.Entity<ContractYear>().HasKey(x => x.ContractYearID);
		modelBuilder.Entity<ContractYear>().Property(x => x.ContractYearID).ValueGeneratedOnAdd();
		modelBuilder.Entity<ContractYear>()
			.HasOne(x => x.Contract)
			.WithMany(x => x.YearlyBreakdown)
			.HasForeignKey(x => x.ContractID);
		modelBuilder.Entity<ContractYear>()
			.HasOne(x => x.Team)
			.WithMany()
			.HasForeignKey(x => x.TeamID);
	}
}
