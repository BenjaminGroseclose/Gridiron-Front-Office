using GridironFrontOffice.Application.Saves;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>This app's schema version: the number of EF migrations it ships (PERSISTENCE.md §7).</summary>
internal static class LeagueSchema
{
    private static readonly Lazy<IReadOnlyList<string>> Migrations = new(() =>
    {
        // Listing migrations reads the assembly; the in-memory connection string is never opened.
        using var db = new LeagueDbContext(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=:memory:").Options);
        return db.Database.GetMigrations().Order(StringComparer.Ordinal).ToList();
    });

    public static int CurrentVersion => Migrations.Value.Count;

    /// <summary>
    /// What a save's applied migrations say about the app that made it, or null when this app ships every one of them
    /// (the save is then ready, or needs an upgrade when it has fewer). A migration ID starts with the UTC time it was
    /// generated, so an unknown one before this app's newest was regenerated away (D-090: FromOlderBuild), and one
    /// after it, or a higher user_version, comes from a newer app.
    /// </summary>
    public static SaveCardStatus? Mismatch(IEnumerable<string> applied, int userVersion)
    {
        var known = Migrations.Value;
        var unknown = applied.Where(id => !known.Contains(id, StringComparer.Ordinal)).ToList();
        if (userVersion > known.Count || unknown.Exists(id => string.CompareOrdinal(id, known[known.Count - 1]) > 0))
        {
            return SaveCardStatus.FromNewerVersion;
        }

        return unknown.Count > 0 ? SaveCardStatus.FromOlderBuild : null;
    }
}
