using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

/// <summary>PERSISTENCE.md §3: per-connection pragmas, applied on every open (pooled connections included).</summary>
internal sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    public const string Pragmas =
        "PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY; PRAGMA cache_size = -32000; PRAGMA busy_timeout = 5000;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
