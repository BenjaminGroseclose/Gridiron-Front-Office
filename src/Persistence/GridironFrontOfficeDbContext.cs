using System.Text;
using GridironFrontOffice.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GridironFrontOffice.Persistence;

public class GridironFrontOfficeDbContext : DbContext
{
	public GridironFrontOfficeDbContext(DbContextOptions<GridironFrontOfficeDbContext> options) : base(options)
	{
	}

	public DbSet<LeagueSetting> LeagueSettings => Set<LeagueSetting>();
	public DbSet<Player> Players => Set<Player>();
	public DbSet<Stadium> Stadiums => Set<Stadium>();
	public DbSet<Team> Teams => Set<Team>();
	public DbSet<Game> Games => Set<Game>();
	public DbSet<Season> Seasons => Set<Season>();
	public DbSet<TeamSeason> TeamSeasons => Set<TeamSeason>();
	public DbSet<Week> Weeks => Set<Week>();
	public DbSet<GameStats> GameStats => Set<GameStats>();
	public DbSet<DraftPick> DraftPicks => Set<DraftPick>();
	public DbSet<Draft> Drafts => Set<Draft>();
	public DbSet<Contract> Contracts => Set<Contract>();
	public DbSet<ContractYear> ContractYears => Set<ContractYear>();
	public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<LeagueSetting>().HasKey(x => x.LeagueSettingID);
		modelBuilder.Entity<LeagueSetting>().Property(x => x.LeagueSettingID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Player>().HasKey(x => x.PlayerID);
		modelBuilder.Entity<Player>().Property(x => x.PlayerID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Player>()
			.HasMany(x => x.Contracts)
			.WithOne(x => x.Player)
			.HasForeignKey(x => x.PlayerID);

		modelBuilder.Entity<Stadium>().HasKey(x => x.StadiumID);
		modelBuilder.Entity<Stadium>().Property(x => x.StadiumID).ValueGeneratedOnAdd();

		modelBuilder.Entity<Team>().HasKey(x => x.TeamID);
		modelBuilder.Entity<Team>().Property(x => x.TeamID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Team>()
			.HasOne(x => x.Stadium)
			.WithMany()
			.HasForeignKey(x => x.StadiumID);

		modelBuilder.Entity<Season>().HasKey(x => x.SeasonID);

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
		modelBuilder.Entity<Game>()
			.HasOne(x => x.HomeTeam)
			.WithMany()
			.HasForeignKey(x => x.HomeTeamID)
			.OnDelete(DeleteBehavior.NoAction);
		modelBuilder.Entity<Game>()
			.HasOne(x => x.AwayTeam)
			.WithMany()
			.HasForeignKey(x => x.AwayTeamID)
			.OnDelete(DeleteBehavior.NoAction);

		modelBuilder.Entity<TeamSeason>().HasKey(x => x.TeamSeasonID);
		modelBuilder.Entity<TeamSeason>().Property(x => x.TeamSeasonID).ValueGeneratedOnAdd();
		modelBuilder.Entity<TeamSeason>()
			.HasOne(x => x.Team)
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
		modelBuilder.Entity<Contract>()
			.HasOne(x => x.Player)
			.WithMany(x => x.Contracts)
			.HasForeignKey(x => x.PlayerID);
		modelBuilder.Entity<Contract>()
			.Navigation(x => x.YearlyBreakdown)
			.AutoInclude();

		modelBuilder.Entity<ContractYear>().HasKey(x => x.ContractYearID);
		modelBuilder.Entity<ContractYear>().Property(x => x.ContractYearID).ValueGeneratedOnAdd();
		modelBuilder.Entity<ContractYear>()
			.HasOne(x => x.Contract)
			.WithMany(x => x.YearlyBreakdown)
			.HasForeignKey(x => x.ContractID);
		modelBuilder.Entity<ContractYear>()
			.HasOne(x => x.Team)
			.WithMany(x => x.ContractYears)
			.HasForeignKey(x => x.TeamID);
		modelBuilder.Entity<ContractYear>()
			.HasOne<Season>()
			.WithMany()
			.HasForeignKey(x => x.SeasonID);


		modelBuilder.Entity<DraftPick>().HasKey(x => x.DraftPickID);
		modelBuilder.Entity<DraftPick>().Property(x => x.DraftPickID).ValueGeneratedOnAdd();
		modelBuilder.Entity<DraftPick>()
			.HasOne(x => x.Team)
			.WithMany()
			.HasForeignKey(x => x.TeamID);

		modelBuilder.Entity<DraftPick>()
			.HasOne<Season>()
			.WithMany()
			.HasForeignKey(x => x.SeasonID);

		modelBuilder.Entity<Draft>().HasKey(x => x.DraftID);
		modelBuilder.Entity<Draft>().Property(x => x.DraftID).ValueGeneratedOnAdd();
		modelBuilder.Entity<Draft>()
			.HasOne(x => x.Season)
			.WithMany()
			.HasForeignKey(x => x.SeasonID);

		modelBuilder.Entity<PlayerStats>().HasKey(x => x.PlayerStatsID);
		modelBuilder.Entity<PlayerStats>().Property(x => x.PlayerStatsID).ValueGeneratedOnAdd();
		modelBuilder.Entity<PlayerStats>()
			.HasOne(x => x.Player)
			.WithMany()
			.HasForeignKey(x => x.PlayerID);
		modelBuilder.Entity<PlayerStats>()
			.HasOne(x => x.Game)
			.WithMany()
			.HasForeignKey(x => x.GameID);
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await base.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase) == true)
		{
			var details = BuildForeignKeyDiagnostics();
			throw new DbUpdateException(
				$"FOREIGN KEY constraint failed. Tracked entity details:\n{details}", ex.InnerException);
		}
	}

	public override int SaveChanges()
	{
		try
		{
			return base.SaveChanges();
		}
		catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase) == true)
		{
			var details = BuildForeignKeyDiagnostics();
			throw new DbUpdateException(
				$"FOREIGN KEY constraint failed. Tracked entity details:\n{details}", ex.InnerException);
		}
	}

	private string BuildForeignKeyDiagnostics()
	{
		var sb = new StringBuilder();
		var entries = ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
			.ToList();

		foreach (var entry in entries)
		{
			sb.AppendLine($"  Entity: {entry.Metadata.ClrType.Name} | State: {entry.State}");

			var fkProperties = entry.Metadata.GetForeignKeys();
			foreach (var fk in fkProperties)
			{
				var principalEntity = fk.PrincipalEntityType.ClrType.Name;
				var fkProps = fk.Properties.Select(p =>
				{
					var currentValue = entry.Property(p.Name).CurrentValue;
					return $"{p.Name}={currentValue ?? "null"}";
				});
				sb.AppendLine($"    FK -> {principalEntity} ({string.Join(", ", fkProps)})");
			}
		}

		if (entries.Count == 0)
		{
			sb.AppendLine("  (No tracked entities with pending changes)");
		}

		return sb.ToString();
	}
}
