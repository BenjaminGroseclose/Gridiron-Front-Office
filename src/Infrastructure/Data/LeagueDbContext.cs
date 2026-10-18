using System.Diagnostics.CodeAnalysis;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Ids;
using GridironFrontOffice.Core.Leagues;
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

    IQueryable<SaveInfo> ILeagueReadDb.SaveInfo => SaveInfo;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // D-015: every strongly typed ID in Core maps to INTEGER through one converter.
        TypedIds.ApplyConversions(configurationBuilder, TypedIds.DeclaredIn(typeof(IStronglyTypedId).Assembly));

        // Enums are stored as their names, so reordering an enum can never change what an existing save means.
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeagueDbContext).Assembly);   // Fluent API keeps Core EF-free
        TypedIds.UseAutoincrementForGeneratedKeys(modelBuilder);
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a running app (Data/Migrations).</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by the EF Core design-time tools.")]
internal sealed class DesignTimeLeagueDbContextFactory : IDesignTimeDbContextFactory<LeagueDbContext>
{
    public LeagueDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=design-time.gfo").Options);
}
