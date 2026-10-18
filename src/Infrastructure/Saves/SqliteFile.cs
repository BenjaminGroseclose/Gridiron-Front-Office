using System.Globalization;
using Microsoft.Data.Sqlite;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>
/// File-level SQLite work that happens outside commands (PERSISTENCE.md §3, §7): the format check before a save is
/// opened, the persistent pragmas at creation, VACUUM INTO, and releasing the file at close. Raw connections are
/// allowed here: this is Infrastructure, and none of it is a transaction.
/// </summary>
internal static class SqliteFile
{
    /// <summary>PRAGMA application_id = 'GFO1': how GFO recognizes its own files.</summary>
    public const int ApplicationId = 0x47464F31;

    /// <summary>What the format check found.</summary>
    /// <param name="IsDatabase">False when the file isn't SQLite at all.</param>
    /// <param name="QuickCheck">"ok", or the first problem quick_check reported.</param>
    public sealed record Probe(bool IsDatabase, int ApplicationId, int UserVersion, string QuickCheck);

    /// <summary>A read-only look at a file: is it SQLite, is it ours, which schema, is it intact. Never writes.</summary>
    public static Probe Check(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var applicationId = Convert.ToInt32(Scalar(connection, "PRAGMA application_id;"), CultureInfo.InvariantCulture);
            var userVersion = Convert.ToInt32(Scalar(connection, "PRAGMA user_version;"), CultureInfo.InvariantCulture);
            var quickCheck = Convert.ToString(Scalar(connection, "PRAGMA quick_check(1);"), CultureInfo.InvariantCulture) ?? "no result";
            return new Probe(IsDatabase: true, applicationId, userVersion, quickCheck);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 26)   // SQLITE_NOTADB
        {
            return new Probe(IsDatabase: false, 0, 0, "not a database");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 11)   // SQLITE_CORRUPT
        {
            return new Probe(IsDatabase: true, ApplicationId, 0, ex.Message);
        }
    }

    /// <summary>
    /// The pragmas that live in the file, set once on a brand-new database before any table exists: page size, WAL
    /// (readers never block the writer) and the GFO signature.
    /// </summary>
    public static void InitializeNew(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, "PRAGMA page_size = 4096;");
        Execute(connection, "PRAGMA journal_mode = WAL;");
        Execute(connection, $"PRAGMA application_id = {ApplicationId};");
    }

    /// <summary>Mirrors the schema version in the file header, so the load screen can read it without EF.</summary>
    public static void SetUserVersion(string connectionString, int version)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {version};"));
    }

    /// <summary>A consistent, compact single-file copy of a live save, without closing it (snapshots, the pre-upgrade backup).</summary>
    public static void VacuumInto(string connectionString, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            File.Delete(destination);   // VACUUM INTO refuses to overwrite
        }

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $destination;";
        command.Parameters.AddWithValue("$destination", destination);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// PERSISTENCE.md §3: fold the WAL back into the file, then clear the connection pool. Microsoft.Data.Sqlite keeps
    /// pooled connections (and so the file) open until the pool is cleared; delete, rename and restore fail otherwise.
    /// </summary>
    public static void CheckpointAndRelease(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, "PRAGMA wal_checkpoint(TRUNCATE);");
        SqliteConnection.ClearPool(connection);
    }

    /// <summary>Releases the file without touching it (after a failed open or create).</summary>
    public static void Release(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        SqliteConnection.ClearPool(connection);
    }

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
