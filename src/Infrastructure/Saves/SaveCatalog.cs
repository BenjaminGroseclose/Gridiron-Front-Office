using System.Globalization;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>
/// The load screen's list (PERSISTENCE.md §9). Each file is read through a read-only connection that leaves nothing
/// behind: the GFO signature and schema from the header, then the card's columns from SaveInfo with raw SQL, so a
/// save from an older or newer app (whose SaveInfo has other columns) still gets a card. No quick_check: it reads the
/// whole file, too slow for a list. A damaged save is caught when it's opened.
/// </summary>
internal sealed class SaveCatalog(IAppPaths paths, ILogger<SaveCatalog> log) : ISaveCatalog
{
    // The catalog's columns. They're read from saves made by any version, so they're never renamed or removed.
    private const string CardQuery =
        "SELECT SaveID, LeagueName, CreatedAtUtc, LastPlayedAtUtc, PlayTimeSeconds, AppVersionLastOpened, IsIronman FROM SaveInfo LIMIT 1;";

    public Task<IReadOnlyList<SaveCard>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => List(cancellationToken), cancellationToken);

    private IReadOnlyList<SaveCard> List(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(paths.Saves))
        {
            return [];
        }

        var cards = new List<SaveCard>();
        foreach (var file in Directory.EnumerateFiles(paths.Saves, "*.gfo"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // "*.gfo" also matches longer extensions on Windows (".gfo-wal"); keep exactly ".gfo".
            if (string.Equals(Path.GetExtension(file), ".gfo", StringComparison.OrdinalIgnoreCase))
            {
                cards.Add(Read(file));
            }
        }

        return
        [
            .. cards.Where(c => c.Status != SaveCardStatus.Unreadable).OrderByDescending(c => c.LastPlayedAtUtc),
            .. cards.Where(c => c.Status == SaveCardStatus.Unreadable).OrderBy(c => c.FileName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <summary>One file's card. Never throws for a bad file: that's what the Unreadable card is for.</summary>
    public SaveCard Read(string path)
    {
        long size = 0;
        try
        {
            size = new FileInfo(path).Length;
            using var connection = new SqliteConnection(SqliteFile.ReadOnly(path));
            connection.Open();
            if (Integer(connection, "PRAGMA application_id;") != SqliteFile.ApplicationID)
            {
                return Unreadable(path, size, "This file isn't a Gridiron Front Office save.");
            }

            var schema = Integer(connection, "PRAGMA user_version;");
            using var command = connection.CreateCommand();
            command.CommandText = CardQuery;
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return Unreadable(path, size, "This save has no league in it.");
            }

            var status = schema.CompareTo(LeagueSchema.CurrentVersion) switch
            {
                < 0 => SaveCardStatus.NeedsUpgrade,
                > 0 => SaveCardStatus.FromNewerVersion,
                _ => SaveCardStatus.Ready,
            };

            return new SaveCard(path, status)
            {
                SaveID = reader.GetGuid(0),
                LeagueName = reader.GetString(1),
                CreatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                LastPlayedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
                PlayTimeSeconds = reader.GetInt64(4),
                AppVersionLastOpened = reader.GetString(5),
                IsIronman = reader.GetBoolean(6),
                SchemaVersion = schema,
                SizeBytes = size,
            };
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 26)   // SQLITE_NOTADB
        {
            return Unreadable(path, size, "This file isn't a Gridiron Front Office save.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 11)   // SQLITE_CORRUPT
        {
            return Unreadable(path, size, "This save is damaged.");
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or FormatException or InvalidCastException)
        {
            return Unreadable(path, size, "This save couldn't be read: " + ex.Message);
        }
    }

    private SaveCard Unreadable(string path, long size, string problem)
    {
        var fileName = Path.GetFileName(path);
        DataLog.SaveUnreadable(log, fileName, problem);
        return new SaveCard(path, SaveCardStatus.Unreadable) { SizeBytes = size, Problem = problem };
    }

    private static int Integer(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
