using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>This app's schema version: the number of EF migrations it ships (PERSISTENCE.md §7).</summary>
internal static class LeagueSchema
{
    private static readonly Lazy<int> Version = new(() =>
    {
        // Listing migrations reads the assembly; the in-memory connection string is never opened.
        using var db = new LeagueDbContext(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=:memory:").Options);
        return db.Database.GetMigrations().Count();
    });

    public static int CurrentVersion => Version.Value;
}
