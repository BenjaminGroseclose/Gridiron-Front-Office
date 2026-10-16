using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

/// <summary>DATA_ACCESS_GUIDE.md §3.3: one context type, created per command or query, never registered in DI.</summary>
internal sealed class LeagueDbContext(DbContextOptions<LeagueDbContext> options)
    : DbContext(options), ILeagueDb, ILeagueReadDb
{
    /// <summary>The 60 wide stat columns of PlayerGameStats (mostly zeros, like the real table).</summary>
    public static readonly IReadOnlyList<string> StatColumns =
        [.. Enumerable.Range(1, 60).Select(i => "Stat" + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture))];

    private static readonly Type[] IdTypes =
        [.. typeof(IStronglyTypedId).Assembly.GetTypes()
            .Where(t => t.IsValueType && typeof(IStronglyTypedId).IsAssignableFrom(t))];

    public DbSet<SaveInfo> SaveInfo => Set<SaveInfo>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractYear> ContractYears => Set<ContractYear>();
    public DbSet<TransactionLogEntry> TransactionLog => Set<TransactionLogEntry>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<TeamSeason> TeamSeasons => Set<TeamSeason>();
    public DbSet<PlayerGameStats> PlayerGameStats => Set<PlayerGameStats>();
    public DbSet<GamePlayLog> GamePlayLogs => Set<GamePlayLog>();

    IQueryable<SaveInfo> ILeagueReadDb.SaveInfo => SaveInfo;
    IQueryable<Team> ILeagueReadDb.Teams => Teams;
    IQueryable<Player> ILeagueReadDb.Players => Players;
    IQueryable<Contract> ILeagueReadDb.Contracts => Contracts;
    IQueryable<TransactionLogEntry> ILeagueReadDb.TransactionLog => TransactionLog;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // D-015: every strongly typed ID maps to INTEGER through one generic converter.
        foreach (var idType in IdTypes)
        {
            configurationBuilder.Properties(idType).HaveConversion(typeof(IdConverter<>).MakeGenericType(idType));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SaveInfo>(e =>
        {
            e.ToTable("SaveInfo");
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => x.Abbreviation).IsUnique();
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId);
            e.HasMany(x => x.Contracts).WithOne().HasForeignKey(c => c.PlayerId);
            e.HasIndex(x => new { x.TeamId, x.Status });
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId);
            e.HasMany(x => x.Years).WithOne(y => y.Contract).HasForeignKey(y => y.ContractId);
        });

        modelBuilder.Entity<ContractYear>(e => e.HasIndex(x => new { x.ContractId, x.Season }).IsUnique());

        modelBuilder.Entity<TransactionLogEntry>(e => e.ToTable("TransactionLog"));

        modelBuilder.Entity<Game>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.Season, x.Week });
        });

        modelBuilder.Entity<PlayerGameStats>(e =>
        {
            e.HasKey(x => new { x.GameId, x.PlayerId });
            e.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId);
            e.HasIndex(x => x.PlayerId);
            foreach (var column in StatColumns)
            {
                e.Property<int>(column);
            }
        });

        modelBuilder.Entity<GamePlayLog>(e =>
        {
            e.HasKey(x => x.GameId);
            e.HasOne<Game>().WithOne().HasForeignKey<GamePlayLog>(x => x.GameId);
        });

        modelBuilder.Entity<TeamSeason>(e => e.HasIndex(x => new { x.TeamId, x.Season }).IsUnique());

        UseAutoincrementForTypedIdKeys(modelBuilder);
    }

    /// <summary>
    /// Spike finding: EF's SQLite convention adds AUTOINCREMENT only to plain integer keys. A key behind a value
    /// converter (a typed ID) gets a bare INTEGER PRIMARY KEY, which SQLite may reuse after the highest row is deleted.
    /// IDs are referenced from history, so every generated typed-ID key gets AUTOINCREMENT explicitly, through
    /// EF Core 10's value-generation strategy API (a raw "Sqlite:Autoincrement" annotation is ignored in EF 10).
    /// </summary>
    private static void UseAutoincrementForTypedIdKeys(ModelBuilder modelBuilder)
    {
        var generatedTypedIdKeys = modelBuilder.Model.GetEntityTypes()
            .Select(entity => (Entity: entity, Key: entity.FindPrimaryKey()))
            .Where(x => x.Key is { Properties.Count: 1 })
            .Select(x => (x.Entity, Property: x.Key!.Properties[0]))
            .Where(x => typeof(IStronglyTypedId).IsAssignableFrom(x.Property.ClrType)
                        && x.Property.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd)
            .ToList();

        foreach (var (entity, property) in generatedTypedIdKeys)
        {
            modelBuilder.Entity(entity.ClrType).Property(property.Name).UseAutoincrement();
        }
    }
}
