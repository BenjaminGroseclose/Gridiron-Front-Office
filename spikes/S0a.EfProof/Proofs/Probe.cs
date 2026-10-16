using System.Globalization;
using GridironFrontOffice.Spikes.S0a.EfProof.Data;
using Microsoft.Data.Sqlite;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

/// <summary>Independent raw-SQL probes for verifying what's really in the file (outside EF and SaveWriter).</summary>
internal static class Probe
{
    public static async Task<long> ScalarAsync(LeagueSession session, string sql, CancellationToken ct) =>
        Convert.ToInt64(await RawScalarAsync(session, sql, ct), CultureInfo.InvariantCulture);

    public static async Task<string?> TextAsync(LeagueSession session, string sql, CancellationToken ct) =>
        Convert.ToString(await RawScalarAsync(session, sql, ct), CultureInfo.InvariantCulture);

    public static long FileSize(string path) =>
        (File.Exists(path) ? new FileInfo(path).Length : 0) + (File.Exists(path + "-wal") ? new FileInfo(path + "-wal").Length : 0);

    private static async Task<object?> RawScalarAsync(LeagueSession session, string sql, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(session.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }
}
