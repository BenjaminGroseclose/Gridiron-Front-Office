using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The one context type (DATA_ACCESS_GUIDE.md §3.3). SaveWriter and SaveReader create one per command or query from
/// the open session's options and dispose it straight away. It's never registered in DI.
/// </summary>
internal sealed class LeagueDbContext(DbContextOptions<LeagueDbContext> options)
    : DbContext(options), ILeagueDb, ILeagueReadDb
{
    public DbSet<SaveInfo> SaveInfo => Set<SaveInfo>();

    public DbSet<Conference> Conferences => Set<Conference>();

    public DbSet<Division> Divisions => Set<Division>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<College> Colleges => Set<College>();

    public DbSet<LeagueSeason> Seasons => Set<LeagueSeason>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ContractYear> ContractYears => Set<ContractYear>();

    IQueryable<SaveInfo> ILeagueReadDb.SaveInfo => SaveInfo;

    IQueryable<Conference> ILeagueReadDb.Conferences => Conferences;

    IQueryable<Division> ILeagueReadDb.Divisions => Divisions;

    IQueryable<Team> ILeagueReadDb.Teams => Teams;

    IQueryable<College> ILeagueReadDb.Colleges => Colleges;

    IQueryable<LeagueSeason> ILeagueReadDb.Seasons => Seasons;

    IQueryable<Player> ILeagueReadDb.Players => Players;

    IQueryable<Contract> ILeagueReadDb.Contracts => Contracts;

    IQueryable<ContractYear> ILeagueReadDb.ContractYears => ContractYears;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // D-015: every strongly typed ID in Core maps to INTEGER through one converter.
        TypedIDs.ApplyConversions(configurationBuilder, TypedIDs.DeclaredIn(typeof(IStronglyTypedID).Assembly));

        // Enums are stored as their names, so reordering an enum can never change what an existing save means.
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeagueDbContext).Assembly);   // Fluent API keeps Core EF-free

        // History is kept forever, so a delete never cascades (WP5 §2)
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        TypedIDs.UseAutoincrementForTypedIDKeys(modelBuilder);
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a running app (Data/Migrations).</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by the EF Core design-time tools.")]
internal sealed class DesignTimeLeagueDbContextFactory : IDesignTimeDbContextFactory<LeagueDbContext>
{
    public LeagueDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=design-time.gfo").Options);
}
