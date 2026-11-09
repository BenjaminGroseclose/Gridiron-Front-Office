using System.Globalization;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>
/// The load screen's list (PERSISTENCE.md §9). Each file is read through a read-only connection that leaves nothing
/// behind: the GFO signature and schema from the header, the migration IDs, then the card's columns from SaveInfo with raw SQL, so a
/// save from an older or newer app (whose SaveInfo has other columns) still gets a card. No quick_check: it reads the
/// whole file, too slow for a list. A damaged save is caught when it's opened.
/// </summary>
internal sealed class SaveCatalog(IAppPaths paths, ILogger<SaveCatalog> log) : ISaveCatalog
{
    // The catalog's columns. They're read from saves made by any version, so they're never renamed or removed.
    private const string CardQuery =
        "SELECT SaveID, LeagueName, CreatedAtUtc, LastPlayedAtUtc, PlayTimeSeconds, AppVersionLastOpened, IsIronman FROM SaveInfo LIMIT 1;";

    // The league's position and the user's team (M1 WP8, F-01 §4). Read only from a Ready save: another version's tables
    // may differ. The Teams count tells a league from a save made by M0's name-only New League, which has none.
    private const string LeagueQuery =
        "SELECT s.CurrentSeason, s.CurrentPhase, (SELECT COUNT(*) FROM Teams), t.TeamID, t.Abbreviation, t.City, t.Nickname, " +
        "t.PrimaryColor, t.SecondaryColor, t.AccentColor, t.CrestShape, t.CrestMonogram, t.CrestStripe " +
        "FROM SaveInfo s LEFT JOIN Teams t ON t.TeamID = s.UserTeamID LIMIT 1;";

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
            var mismatch = LeagueSchema.Mismatch(MigrationIDs(connection), schema);
            SaveCard card;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = CardQuery;
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    return Unreadable(path, size, "This save has no league in it.");
                }

                // OpenAsync's check, so the list never offers a save that can't be opened
                var status = mismatch ?? (schema < LeagueSchema.CurrentVersion ? SaveCardStatus.NeedsUpgrade : SaveCardStatus.Ready);

                card = new SaveCard(path, status)
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

            return card.Status == SaveCardStatus.Ready ? WithLeague(connection, card) : card;
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

    /// <summary>
    /// The card with its league's season, phase and user team. A save with no league in it, or a phase or crest name
    /// this build doesn't know, keeps the card as it is: one odd save mustn't break the load screen (Enum.Parse throws
    /// ArgumentException, which the catch above doesn't list).
    /// </summary>
    private static SaveCard WithLeague(SqliteConnection connection, SaveCard card)
    {
        using var command = connection.CreateCommand();
        command.CommandText = LeagueQuery;
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(2) == 0 || !Known(reader.GetString(1), out Phase phase))
        {
            return card;
        }

        TeamIdentityDto? team = null;
        if (!reader.IsDBNull(3))
        {
            if (!Known(reader.GetString(10), out CrestShape shape) || !Known(reader.GetString(12), out CrestStripe stripe))
            {
                return card;
            }

            team = new TeamIdentityDto(new TeamID(reader.GetInt32(3)), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetString(7), reader.GetString(8), reader.GetString(9), shape, reader.GetString(11), stripe);
        }

        return card with { Season = reader.GetInt32(0), Phase = phase, Team = team };
    }

    // A name the enum defines: TryParse alone also takes numbers ("42") and gives values the enum doesn't have
    private static bool Known<T>(string text, out T value)
        where T : struct, Enum => Enum.TryParse(text, out value) && Enum.IsDefined(value);

    private SaveCard Unreadable(string path, long size, string problem)
    {
        var fileName = Path.GetFileName(path);
        DataLog.SaveUnreadable(log, fileName, problem);
        return new SaveCard(path, SaveCardStatus.Unreadable) { SizeBytes = size, Problem = problem };
    }

    /// <summary>
    /// The save's applied migrations. The schema version alone can't tell a save from an older development build
    /// before 1.0, since the regenerated InitialCreate keeps the count at 1 (D-090).
    /// </summary>
    private static List<string> MigrationIDs(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";
        using var reader = command.ExecuteReader();
        var ids = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static int Integer(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
