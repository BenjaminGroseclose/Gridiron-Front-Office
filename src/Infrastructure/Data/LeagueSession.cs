using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The open save: its path, connection string and EF options (DATA_ACCESS_GUIDE.md §3.3). Built when a save is opened,
/// dropped when it's closed. No AddDbContext anywhere: the file changes when the player loads another league.
/// </summary>
internal sealed class LeagueSession
{
    private LeagueSession(string savePath, string connectionString, DbContextOptions<LeagueDbContext> options) =>
        (SavePath, ConnectionString, Options) = (savePath, connectionString, options);

    public string SavePath { get; }

    public string ConnectionString { get; }

    public DbContextOptions<LeagueDbContext> Options { get; }

    /// <summary>PERSISTENCE.md §3. ReadWrite, not ReadWriteCreate: a save that vanished must fail, not reappear empty.</summary>
    public static LeagueSession For(string path, SqlitePragmaInterceptor pragmas)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = true,
            ForeignKeys = true,
            DefaultTimeout = 30,
        }.ToString();

        var options = new DbContextOptionsBuilder<LeagueDbContext>()
            .UseSqlite(connectionString)
            .AddInterceptors(pragmas)
            .Options;
        return new LeagueSession(path, connectionString, options);
    }
}

/// <summary>Holds the open session for SaveWriter and SaveReader. Only SaveManager changes it.</summary>
internal sealed class LeagueSessionAccessor
{
    private volatile LeagueSession? _current;

    /// <summary>The open session. Using the data layer with no save open is a bug, so it throws.</summary>
    public LeagueSession Current =>
        _current ?? throw new InvalidOperationException("No save is open. Open or create one with ISaveManager first.");

    public LeagueSession? CurrentOrNull => _current;

    public void Set(LeagueSession? session) => _current = session;
}

/// <summary>PERSISTENCE.md §3: the per-connection pragmas, on every open (pooled connections included).</summary>
internal sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    public const string Pragmas =
        "PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY; PRAGMA cache_size = -32000; PRAGMA busy_timeout = 5000;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(connection);
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = Pragmas;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
