using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

/// <summary>DATA_ACCESS_GUIDE.md §3.3: the open save, holding its path and EF options. No AddDbContext anywhere.</summary>
internal sealed class LeagueSession
{
    private static readonly SqlitePragmaInterceptor PragmaInterceptor = new();

    public required string SavePath { get; init; }
    public required string ConnectionString { get; init; }
    public required DbContextOptions<LeagueDbContext> Options { get; init; }

    public static LeagueSession For(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = 30,
        }.ToString();

        return new LeagueSession
        {
            SavePath = path,
            ConnectionString = connectionString,
            Options = new DbContextOptionsBuilder<LeagueDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(PragmaInterceptor)
                .Options,
        };
    }
}

/// <summary>The bits of SaveManager the spike needs: create (WAL + migrate), and close (checkpoint + release the file).</summary>
internal static class SaveFile
{
    /// <param name="targetMigration">Null migrates to the latest; a name stops there (to simulate an old save).</param>
    public static async Task<LeagueSession> CreateAsync(string path, string? targetMigration, CancellationToken ct)
    {
        var session = LeagueSession.For(path);
        await using var db = new LeagueDbContext(session.Options);
        await db.Database.OpenConnectionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", ct);   // persistent: set once at creation
        await db.GetService<IMigrator>().MigrateAsync(targetMigration, ct);
        await db.Database.CloseConnectionAsync();
        return session;
    }

    public static async Task MigrateToLatestAsync(LeagueSession session, CancellationToken ct)
    {
        await using var db = new LeagueDbContext(session.Options);
        await db.GetService<IMigrator>().MigrateAsync(null, ct);
    }

    /// <summary>PERSISTENCE.md §3: checkpoint, then clear the pool, or the file stays locked.</summary>
    public static async Task CloseAsync(LeagueSession session, CancellationToken ct)
    {
        await using (var connection = new SqliteConnection(session.ConnectionString))
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await command.ExecuteNonQueryAsync(ct);
            SqliteConnection.ClearPool(connection);
        }
    }

    /// <summary>A consistent single-file copy with VACUUM INTO (snapshots, the pre-upgrade backup).</summary>
    public static async Task VacuumIntoAsync(LeagueSession session, string destination, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(session.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $path;";
        command.Parameters.AddWithValue("$path", destination);
        await command.ExecuteNonQueryAsync(ct);
    }
}

/// <summary>Lets `dotnet ef migrations add` build the model without a running app.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by the EF Core design-time tools.")]
internal sealed class DesignTimeLeagueDbContextFactory : IDesignTimeDbContextFactory<LeagueDbContext>
{
    public LeagueDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=design-time.gfo").Options);
}
