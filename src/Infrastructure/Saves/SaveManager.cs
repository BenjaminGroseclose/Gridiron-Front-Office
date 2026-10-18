using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Data;
using GridironFrontOffice.Infrastructure.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>
/// Creates, opens and closes saves (PERSISTENCE.md §3, §7). The schema version is the number of EF migrations the
/// file has; a file that has a migration this app doesn't know is from a newer app and is refused.
/// </summary>
internal sealed class SaveManager(
    IAppPaths paths,
    AppIdentity app,
    LeagueSessionAccessor sessions,
    SaveWriter writer,
    DomainEventDispatcher events,
    SqlitePragmaInterceptor pragmas,
    TimeProvider time,
    ILogger<SaveManager> log) : ISaveManager, IDisposable
{
    public const int MaxLeagueNameLength = 60;

    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private long _openedAt;

    public OpenSave? Current { get; private set; }

    public async Task<Result<OpenSave>> CreateAsync(string leagueName, CancellationToken cancellationToken = default)
    {
        var name = leagueName?.Trim() ?? "";
        if (name.Length == 0)
        {
            return Result.Fail<OpenSave>(SaveErrors.InvalidName("A league needs a name."));
        }

        if (name.Length > MaxLeagueNameLength)
        {
            return Result.Fail<OpenSave>(SaveErrors.InvalidName($"League names are at most {MaxLeagueNameLength} characters."));
        }

        var dispatch = new List<IDomainEvent>();
        OpenSave created;
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseCoreAsync(dispatch, cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(paths.Saves);
            var path = UniquePath(paths.Saves, name);
            var session = LeagueSession.For(path, pragmas);
            var saveId = Guid.NewGuid();
            int schema;
            try
            {
                SqliteFile.InitializeNew(path);
                schema = await MigrateAsync(session, cancellationToken).ConfigureAwait(false);
                SqliteFile.SetUserVersion(session.ConnectionString, schema);
                await writer.RunExclusiveAsync(() => { sessions.Set(session); return Task.CompletedTask; }, cancellationToken)
                    .ConfigureAwait(false);

                var now = time.GetUtcNow().UtcDateTime;
                await writer.ExecuteAsync("Save.Create", (uow, _) =>
                {
                    uow.Db.SaveInfo.Add(new SaveInfo
                    {
                        SaveId = saveId,
                        LeagueName = name,
                        SchemaVersion = schema,
                        AppVersionCreated = app.Display,
                        AppVersionLastOpened = app.Display,
                        CreatedAtUtc = now,
                        LastPlayedAtUtc = now,
                    });
                    return Task.FromResult(Result.Ok());
                }, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Leave nothing half-made behind: release the file and delete it.
                sessions.Set(null);
                SqliteFile.Release(session.ConnectionString);
                DeleteSaveFiles(path);
                throw;
            }

            created = new OpenSave(saveId, name, path, schema, Upgraded: false);
            Current = created;
            _openedAt = time.GetTimestamp();
            DataLog.SaveCreated(log, name, saveId, path);
            dispatch.Add(new SaveOpened(created));
        }
        finally
        {
            _lifecycle.Release();
        }

        events.Dispatch(dispatch);
        return Result.Ok(created);
    }

    public async Task<Result<OpenSave>> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);

        var dispatch = new List<IDomainEvent>();
        Result<OpenSave> outcome;
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseCoreAsync(dispatch, cancellationToken).ConfigureAwait(false);
            outcome = await OpenCoreAsync(fullPath, fileName, cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccess)
            {
                dispatch.Add(new SaveOpened(outcome.Value));
            }
            else
            {
                DataLog.SaveRefused(log, fileName, outcome.Error!.Message);
            }
        }
        finally
        {
            _lifecycle.Release();
        }

        events.Dispatch(dispatch);
        return outcome;
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        var dispatch = new List<IDomainEvent>();
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseCoreAsync(dispatch, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }

        events.Dispatch(dispatch);
    }

    /// <summary>
    /// The app is exiting without a CloseAsync: release the file synchronously so it isn't left locked. The session's
    /// play time isn't recorded on this path; WP9's shutdown calls CloseAsync first.
    /// </summary>
    public void Dispose()
    {
        if (sessions.CurrentOrNull is { } session)
        {
            sessions.Set(null);
            SqliteFile.CheckpointAndRelease(session.ConnectionString);
            Current = null;
        }

        _lifecycle.Dispose();
    }

    private async Task<Result<OpenSave>> OpenCoreAsync(string fullPath, string fileName, CancellationToken ct)
    {
        if (!File.Exists(fullPath))
        {
            return Result.Fail<OpenSave>(SaveErrors.NotFound(fileName));
        }

        var probe = SqliteFile.Check(fullPath);
        if (!probe.IsDatabase || probe.ApplicationId != SqliteFile.ApplicationId)
        {
            return Result.Fail<OpenSave>(SaveErrors.NotAGfoSave(fileName));
        }

        if (!string.Equals(probe.QuickCheck, "ok", StringComparison.Ordinal))
        {
            return Result.Fail<OpenSave>(SaveErrors.Damaged(fileName, probe.QuickCheck));
        }

        var session = LeagueSession.For(fullPath, pragmas);
        try
        {
            var (known, applied) = await MigrationStateAsync(session, ct).ConfigureAwait(false);
            if (applied.Except(known).Any() || probe.UserVersion > known.Count)
            {
                var newerApp = LastOpenedBy(session.ConnectionString);   // before the release: it opens a pooled connection
                SqliteFile.Release(session.ConnectionString);
                return Result.Fail<OpenSave>(SaveErrors.NewerVersion(fileName, newerApp));
            }

            var pending = known.Count - applied.Count;
            if (pending > 0)
            {
                // PERSISTENCE.md §7: snapshot first, then migrate forward.
                var saveId = ReadSaveId(session.ConnectionString);
                var snapshot = Path.Combine(paths.Snapshots, saveId.ToString(), $"pre-upgrade-v{applied.Count}.gfo");
                SqliteFile.VacuumInto(session.ConnectionString, snapshot);
                await MigrateAsync(session, ct).ConfigureAwait(false);
                SqliteFile.SetUserVersion(session.ConnectionString, known.Count);
                DataLog.SaveUpgraded(log, saveId, applied.Count, known.Count, snapshot);
            }

            await writer.RunExclusiveAsync(() => { sessions.Set(session); return Task.CompletedTask; }, ct).ConfigureAwait(false);
            var schema = known.Count;
            var now = time.GetUtcNow().UtcDateTime;
            var opened = await writer.ExecuteAsync("Save.Open", async (uow, token) =>
            {
                var info = await uow.Db.SaveInfo.SingleAsync(token).ConfigureAwait(false);
                info.SchemaVersion = schema;
                info.AppVersionLastOpened = app.Display;
                info.LastPlayedAtUtc = now;
                return Result.Ok(new OpenSave(info.SaveId, info.LeagueName, fullPath, schema, Upgraded: pending > 0));
            }, ct).ConfigureAwait(false);

            Current = opened.Value;
            _openedAt = time.GetTimestamp();
            DataLog.SaveOpened(log, opened.Value.LeagueName, opened.Value.SaveId, schema);
            return opened;
        }
        catch
        {
            sessions.Set(null);
            SqliteFile.Release(session.ConnectionString);
            throw;
        }
    }

    private async Task CloseCoreAsync(List<IDomainEvent> dispatch, CancellationToken ct)
    {
        if (Current is not { } closing || sessions.CurrentOrNull is not { } session)
        {
            return;
        }

        // Record the session, then wait for any running command and release the file.
        var minutes = time.GetElapsedTime(_openedAt).TotalMinutes;
        var seconds = (long)(minutes * 60);
        var now = time.GetUtcNow().UtcDateTime;
        await writer.ExecuteAsync("Save.Close", async (uow, token) =>
        {
            var info = await uow.Db.SaveInfo.SingleAsync(token).ConfigureAwait(false);
            info.PlayTimeSeconds += seconds;
            info.LastPlayedAtUtc = now;
            return Result.Ok();
        }, ct).ConfigureAwait(false);

        await writer.RunExclusiveAsync(() =>
        {
            sessions.Set(null);
            SqliteFile.CheckpointAndRelease(session.ConnectionString);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

        Current = null;
        DataLog.SaveClosed(log, closing.SaveId, minutes);
        dispatch.Add(new SaveClosed(closing.SaveId));
    }

    /// <summary>Applies every migration the file doesn't have yet. Returns the resulting schema version.</summary>
    private static async Task<int> MigrateAsync(LeagueSession session, CancellationToken ct)
    {
        var db = new LeagueDbContext(session.Options);
        await using (db.ConfigureAwait(false))
        {
            await db.Database.MigrateAsync(ct).ConfigureAwait(false);
            return db.Database.GetMigrations().Count();
        }
    }

    private static async Task<(List<string> Known, List<string> Applied)> MigrationStateAsync(LeagueSession session, CancellationToken ct)
    {
        var db = new LeagueDbContext(session.Options);
        await using (db.ConfigureAwait(false))
        {
            var known = db.Database.GetMigrations().ToList();
            var applied = (await db.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToList();
            return (known, applied);
        }
    }

    private static Guid ReadSaveId(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SaveId FROM SaveInfo LIMIT 1;";
        return Guid.Parse((string)command.ExecuteScalar()!);
    }

    /// <summary>Best effort, for the "update the app" message: the newer app may have changed SaveInfo's shape.</summary>
    private static string? LastOpenedBy(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AppVersionLastOpened FROM SaveInfo LIMIT 1;";
            return command.ExecuteScalar() as string;
        }
        catch (SqliteException)
        {
            return null;
        }
    }

    /// <summary>"Wolves Dynasty.gfo", then "Wolves Dynasty (2).gfo", … with characters Windows forbids replaced.</summary>
    private static string UniquePath(string folder, string leagueName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var stem = new string([.. leagueName.Select(c => invalid.Contains(c) ? '_' : c)]).Trim().TrimEnd('.');
        if (stem.Length == 0)
        {
            stem = "League";
        }

        var candidate = Path.Combine(folder, stem + ".gfo");
        for (var n = 2; File.Exists(candidate); n++)
        {
            candidate = Path.Combine(folder, $"{stem} ({n}).gfo");
        }

        return candidate;
    }

    private static void DeleteSaveFiles(string path)
    {
        foreach (var file in new[] { path, path + "-wal", path + "-shm" })
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Best effort after a failed create; the original exception is what matters.
            }
        }
    }
}
