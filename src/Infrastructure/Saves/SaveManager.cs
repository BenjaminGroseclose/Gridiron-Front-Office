using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.LiveEvents;
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
/// Creates, opens, closes, restores and deletes saves (PERSISTENCE.md §2, §3, §7, §10). The schema version is the
/// number of EF migrations the file has; a file that has a migration this app doesn't know is from a newer app and is
/// refused. Every lifecycle change runs under one lock, and its events are dispatched after the lock is released.
/// </summary>
internal sealed class SaveManager(
    IAppPaths paths,
    AppIdentity app,
    LeagueSessionAccessor sessions,
    SaveWriter writer,
    ISaveReader reader,
    DomainEventDispatcher events,
    SqlitePragmaInterceptor pragmas,
    SnapshotService snapshots,
    SaveCatalog catalog,
    IPlatformServices platform,
    ILiveEventStatus liveEvents,
    TimeProvider time,
    ILogger<SaveManager> log) : ISaveManager, IDisposable
{

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

        if (name.Length > SaveRules.MaxLeagueNameLength)
        {
            return Result.Fail<OpenSave>(SaveErrors.InvalidName($"League names are at most {SaveRules.MaxLeagueNameLength} characters."));
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
            var saveID = Guid.NewGuid();
            int schema;
            try
            {
                SqliteFile.InitializeNew(path);
                schema = await MigrateAsync(session, cancellationToken).ConfigureAwait(false);
                SqliteFile.SetUserVersion(session.ConnectionString, schema);
                await writer.RunExclusiveAsync("creating a save", () => { sessions.Set(session); return Task.CompletedTask; }, cancellationToken)
                    .ConfigureAwait(false);

                var now = time.GetUtcNow().UtcDateTime;
                await writer.ExecuteAsync("Save.Create", (uow, _) =>
                {
                    uow.Db.SaveInfo.Add(new SaveInfo
                    {
                        SaveID = saveID,
                        LeagueName = name,
                        SchemaVersion = schema,
                        AppVersionCreated = app.Display,
                        AppVersionLastOpened = app.Display,
                        CreatedAtUtc = now,
                        LastPlayedAtUtc = now,
                    });
                    return Task.FromResult(Result.Ok());
                }, cancellationToken).ConfigureAwait(false);

                await snapshots.TakeAsync(SnapshotTrigger.Created, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Leave nothing half-made behind: release the file and delete it.
                sessions.Set(null);
                SqliteFile.Release(session.ConnectionString);
                DeleteSaveFiles(path);
                throw;
            }

            created = new OpenSave(saveID, name, path, schema, Upgraded: false);
            Current = created;
            _openedAt = time.GetTimestamp();
            DataLog.SaveCreated(log, name, saveID, path);
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

    public async Task<Result<OpenSave>> RestoreAsync(SnapshotInfo snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var dispatch = new List<IDomainEvent>();
        Result<OpenSave> outcome;
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            outcome = await RestoreCoreAsync(snapshot, dispatch, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                DataLog.RestoreRefused(log, snapshot.Name, outcome.Error!.Message);
            }
        }
        finally
        {
            _lifecycle.Release();
        }

        events.Dispatch(dispatch);
        return outcome;
    }

    public async Task<Result<Unit>> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);

        var dispatch = new List<IDomainEvent>();
        Result<Unit> outcome;
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            outcome = await DeleteCoreAsync(fullPath, fileName, dispatch, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                DataLog.DeleteRefused(log, fileName, outcome.Error!.Message);
            }
        }
        finally
        {
            _lifecycle.Release();
        }

        events.Dispatch(dispatch);
        return outcome;
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
        if (!probe.IsDatabase || probe.ApplicationID != SqliteFile.ApplicationID)
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
                var saveID = SqliteFile.ReadSaveID(session.ConnectionString);
                var snapshot = snapshots.Take(session.ConnectionString, saveID, SnapshotTrigger.PreUpgrade(applied.Count));
                await MigrateAsync(session, ct).ConfigureAwait(false);
                SqliteFile.SetUserVersion(session.ConnectionString, known.Count);
                DataLog.SaveUpgraded(log, saveID, applied.Count, known.Count, snapshot.Name);
            }

            await writer.RunExclusiveAsync("opening a save", () => { sessions.Set(session); return Task.CompletedTask; }, ct).ConfigureAwait(false);
            var schema = known.Count;
            var now = time.GetUtcNow().UtcDateTime;
            var opened = await writer.ExecuteAsync("Save.Open", async (uow, token) =>
            {
                var info = await uow.Db.SaveInfo.SingleAsync(token).ConfigureAwait(false);
                info.SchemaVersion = schema;
                info.AppVersionLastOpened = app.Display;
                info.LastPlayedAtUtc = now;
                return Result.Ok(new OpenSave(info.SaveID, info.LeagueName, fullPath, schema, Upgraded: pending > 0));
            }, ct).ConfigureAwait(false);

            Current = opened.Value;
            _openedAt = time.GetTimestamp();
            DataLog.SaveOpened(log, opened.Value.LeagueName, opened.Value.SaveID, schema);
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

        await writer.RunExclusiveAsync("closing a save", () =>
        {
            sessions.Set(null);
            SqliteFile.CheckpointAndRelease(session.ConnectionString);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

        Current = null;
        DataLog.SaveClosed(log, closing.SaveID, minutes);
        dispatch.Add(new SaveClosed(closing.SaveID));
    }

    /// <summary>
    /// PERSISTENCE.md §2.2. The snapshot is checked and staged next to the save before anything is closed, so a bad
    /// snapshot leaves the save open and untouched, and restoring <c>pre-restore</c> itself (undo) works even though a
    /// new <c>pre-restore</c> replaces it.
    /// </summary>
    private async Task<Result<OpenSave>> RestoreCoreAsync(SnapshotInfo snapshot, List<IDomainEvent> dispatch, CancellationToken ct)
    {
        if (Current is not { } open || open.SaveID != snapshot.SaveID)
        {
            return Result.Fail<OpenSave>(SaveErrors.SnapshotNotForThisSave(snapshot.Name));
        }

        var isIronman = await reader.QueryAsync((db, token) => db.SaveInfo.Select(s => s.IsIronman).SingleAsync(token), ct).ConfigureAwait(false);
        var allowed = SaveRules.CanRestore(isIronman, liveEvents.IsRunning);
        if (allowed.IsFailure)
        {
            return Result.Fail<OpenSave>(allowed.Error!);
        }

        if (!File.Exists(snapshot.FilePath))
        {
            return Result.Fail<OpenSave>(SaveErrors.SnapshotNotFound(snapshot.Name));
        }

        var snapshotFile = Path.GetFileName(snapshot.FilePath);
        var probe = SqliteFile.Check(snapshot.FilePath);
        if (!probe.IsDatabase || probe.ApplicationID != SqliteFile.ApplicationID)
        {
            return Result.Fail<OpenSave>(SaveErrors.NotAGfoSave(snapshotFile));
        }

        if (!string.Equals(probe.QuickCheck, "ok", StringComparison.Ordinal))
        {
            return Result.Fail<OpenSave>(SaveErrors.Damaged(snapshotFile, probe.QuickCheck));
        }

        var card = catalog.Read(snapshot.FilePath);
        if (card.Status == SaveCardStatus.Unreadable || card.SaveID != open.SaveID)
        {
            return Result.Fail<OpenSave>(SaveErrors.SnapshotNotForThisSave(snapshot.Name));
        }

        if (card.Status == SaveCardStatus.FromNewerVersion)
        {
            return Result.Fail<OpenSave>(SaveErrors.NewerVersion(snapshotFile, card.AppVersionLastOpened));
        }

        var savePath = open.FilePath;
        var staged = savePath + ".restore-tmp";
        try
        {
            SqliteFile.VacuumInto(SqliteFile.ReadOnly(snapshot.FilePath), staged);   // a copy; the snapshot itself is never touched
            SqliteFile.UseWal(staged);

            await CloseCoreAsync(dispatch, ct).ConfigureAwait(false);
            var playTime = catalog.Read(savePath).PlayTimeSeconds;   // real time played doesn't go back with the league
            snapshots.Take(SqliteFile.ReadOnly(savePath), open.SaveID, SnapshotTrigger.PreRestore);
            foreach (var sidecar in SqliteFile.Sidecars(savePath))
            {
                File.Delete(sidecar);   // a stale WAL must never be replayed onto the restored file
            }

            File.Move(staged, savePath, overwrite: true);

            var reopened = await OpenCoreAsync(savePath, Path.GetFileName(savePath), ct).ConfigureAwait(false);
            if (reopened.IsFailure)
            {
                return reopened;
            }

            await writer.ExecuteAsync("Save.Restored", async (uow, token) =>
            {
                var info = await uow.Db.SaveInfo.SingleAsync(token).ConfigureAwait(false);
                info.PlayTimeSeconds = Math.Max(info.PlayTimeSeconds, playTime);
                return Result.Ok();
            }, ct).ConfigureAwait(false);

            DataLog.SaveRestored(log, open.SaveID, snapshot.Name);
            dispatch.Add(new SaveOpened(reopened.Value));
            return reopened;
        }
        finally
        {
            File.Delete(staged);   // no-op once it has been moved into place
        }
    }

    private async Task<Result<Unit>> DeleteCoreAsync(string fullPath, string fileName, List<IDomainEvent> dispatch, CancellationToken ct)
    {
        if (!File.Exists(fullPath))
        {
            return Result.Fail<Unit>(SaveErrors.NotFound(fileName));
        }

        if (Current is { } open && string.Equals(open.FilePath, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            await CloseCoreAsync(dispatch, ct).ConfigureAwait(false);
        }

        // The snapshot folder goes too, unless another file shares the SaveID (a copy made in Explorer).
        var card = catalog.Read(fullPath);
        Guid? saveID = card.Status == SaveCardStatus.Unreadable ? null : card.SaveID;
        var targets = new List<string> { fullPath };
        targets.AddRange(SqliteFile.Sidecars(fullPath));
        var withSnapshots = false;
        if (saveID is { } id && Directory.Exists(snapshots.FolderOf(id))
            && !(await catalog.ListAsync(ct).ConfigureAwait(false))
                .Any(c => c.SaveID == id && !string.Equals(c.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)))
        {
            targets.Add(snapshots.FolderOf(id));
            withSnapshots = true;
        }

        try
        {
            if (!platform.SendToRecycleBin(targets))
            {
                return Result.Fail<Unit>(SaveErrors.DeleteCancelled(fileName));
            }
        }
        catch (IOException ex)
        {
            return Result.Fail<Unit>(SaveErrors.DeleteFailed(fileName, ex.Message));
        }

        DataLog.SaveDeleted(log, fileName, withSnapshots);
        dispatch.Add(new SaveDeleted(fullPath, saveID));
        return Result.Ok();
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
