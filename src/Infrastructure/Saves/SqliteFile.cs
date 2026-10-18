using System.Globalization;
using Microsoft.Data.Sqlite;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>
/// File-level SQLite work that happens outside commands (PERSISTENCE.md §2, §3, §7): the format check before a save
/// is opened, the persistent pragmas at creation, VACUUM INTO, and releasing the file at close. Raw connections are
/// allowed here: this is Infrastructure, and none of it is a transaction.
/// </summary>
internal static class SqliteFile
{
    /// <summary>PRAGMA application_id = 'GFO1': how GFO recognizes its own files.</summary>
    public const int ApplicationID = 0x47464F31;

    /// <summary>What the format check found.</summary>
    /// <param name="IsDatabase">False when the file isn't SQLite at all.</param>
    /// <param name="QuickCheck">"ok", or the first problem quick_check reported.</param>
    public sealed record Probe(bool IsDatabase, int ApplicationID, int UserVersion, string QuickCheck);

    /// <summary>
    /// A read-only, unpooled connection string that leaves nothing behind. A plain read-only connection to a closed
    /// WAL save creates <c>-wal</c> and <c>-shm</c> files and can't delete them on close, so a closed save (no
    /// <c>-wal</c>) is read as <c>immutable</c> instead. With a <c>-wal</c> present (the save is open, or a crash left
    /// it), the plain connection reads through it and the files were already there.
    /// </summary>
    public static string ReadOnly(string path)
    {
        var dataSource = File.Exists(path + "-wal")
            ? path
            : "file:" + path
                .Replace("%", "%25", StringComparison.Ordinal)   // %, ? and # are special in a URI; escape % first
                .Replace("?", "%3f", StringComparison.Ordinal)
                .Replace("#", "%23", StringComparison.Ordinal)
                .Replace('\\', '/') + "?immutable=1";

        return new SqliteConnectionStringBuilder { DataSource = dataSource, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
    }

    /// <summary>A read-only look at a file: is it SQLite, is it ours, which schema, is it intact. Never writes.</summary>
    public static Probe Check(string path)
    {
        try
        {
            using var connection = new SqliteConnection(ReadOnly(path));
            connection.Open();
            var applicationID = Convert.ToInt32(Scalar(connection, "PRAGMA application_id;"), CultureInfo.InvariantCulture);
            var userVersion = Convert.ToInt32(Scalar(connection, "PRAGMA user_version;"), CultureInfo.InvariantCulture);
            var quickCheck = Convert.ToString(Scalar(connection, "PRAGMA quick_check(1);"), CultureInfo.InvariantCulture) ?? "no result";
            return new Probe(IsDatabase: true, applicationID, userVersion, quickCheck);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 26)   // SQLITE_NOTADB
        {
            return new Probe(IsDatabase: false, 0, 0, "not a database");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 11)   // SQLITE_CORRUPT
        {
            return new Probe(IsDatabase: true, ApplicationID, 0, ex.Message);
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
        Execute(connection, $"PRAGMA application_id = {ApplicationID};");
    }

    /// <summary>
    /// <c>VACUUM INTO</c> writes a rollback-journal file whatever the source uses, so a snapshot that becomes the
    /// live save again is switched back to WAL first.
    /// </summary>
    public static void UseWal(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, "PRAGMA journal_mode = WAL;");
    }

    /// <summary>Mirrors the schema version in the file header, so the load screen can read it without EF.</summary>
    public static void SetUserVersion(string connectionString, int version)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {version};"));
    }

    /// <summary>The save's identity, from its SaveInfo row.</summary>
    public static Guid ReadSaveID(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        return Guid.Parse((string)Scalar(connection, "SELECT SaveID FROM SaveInfo LIMIT 1;")!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A consistent, compact single-file copy of a database without closing it (snapshots, the pre-upgrade backup,
    /// staging a restore). The source may be a live save or a read-only connection.
    /// </summary>
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

    /// <summary>The <c>-wal</c> and <c>-shm</c> files that exist next to a save.</summary>
    public static IEnumerable<string> Sidecars(string path) =>
        new[] { path + "-wal", path + "-shm" }.Where(File.Exists);

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
