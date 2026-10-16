using System.Diagnostics;
using GridironFrontOffice.Spikes.S0a.EfProof.Data;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

/// <summary>
/// The core torture tests from DATA_ACCESS_GUIDE.md §9, run against the spike's SaveWriter. WP6 makes them permanent
/// (plus the kill-the-process test, which needs the Crasher app).
/// </summary>
internal static class TortureChecks
{
    private const string ProbeLogInsert =
        "INSERT INTO TransactionLog (Season, Kind, TeamId, PlayerId) VALUES (2027, 'Probe', 1, 1)";

    public static async Task RunAsync(LeagueSession session, SaveWriter writer, SaveReader reader, Report report, CancellationToken ct)
    {
        await ExceptionMidwayPersistsNothingAsync(session, writer, report, ct);
        await DomainFailurePersistsNothingAsync(session, writer, report, ct);
        await NestedCommandThrowsInsteadOfDeadlockingAsync(writer, report, ct);
        await ConcurrentCommandsAreSerializedAsync(session, writer, report, ct);
        await ReadDuringOpenWriteIsNotBlockedAsync(writer, reader, report, ct);
        await TransactionTakesTheWriteLockAtBeginAsync(session, writer, report, ct);
    }

    private static async Task ExceptionMidwayPersistsNothingAsync(LeagueSession session, SaveWriter writer, Report report, CancellationToken ct)
    {
        var before = await CountsAsync(session, ct);
        var threw = false;
        try
        {
            await writer.ExecuteAsync<Unit>("Torture.ExceptionMidway", async (uow, ct) =>
            {
                uow.Db.Players.Add(ProbePlayer());
                await uow.Db.SaveChangesAsync(ct);                    // flushed, NOT committed
                await using var raw = uow.CreateCommand(ProbeLogInsert);
                await raw.ExecuteNonQueryAsync(ct);                   // raw SQL in the same transaction
                throw new InvalidOperationException("boom");
            }, ct);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        var after = await CountsAsync(session, ct);
        report.Check("Torture: an exception midway persists nothing (EF flush + raw SQL roll back together)",
            threw && after == before, $"(players, log, generation) before {before}, after {after}");
    }

    private static async Task DomainFailurePersistsNothingAsync(LeagueSession session, SaveWriter writer, Report report, CancellationToken ct)
    {
        var before = await CountsAsync(session, ct);
        var result = await writer.ExecuteAsync<Unit>("Torture.DomainFailure", async (uow, ct) =>
        {
            uow.Db.Players.Add(ProbePlayer());
            await uow.Db.SaveChangesAsync(ct);
            await using var raw = uow.CreateCommand(ProbeLogInsert);
            await raw.ExecuteNonQueryAsync(ct);
            return Result<Unit>.Fail(DomainError.Rejected("probe"));
        }, ct);

        var after = await CountsAsync(session, ct);
        report.Check("Torture: Result.Fail after changes persists nothing",
            result.IsFailure && after == before, $"before {before}, after {after}");
    }

    private static async Task NestedCommandThrowsInsteadOfDeadlockingAsync(SaveWriter writer, Report report, CancellationToken ct)
    {
        var nestedThrew = false;
        try
        {
            await writer.ExecuteAsync<Unit>("Torture.Outer", async (_, ct) =>
            {
                await writer.ExecuteAsync<Unit>("Torture.Inner", (_, _) => Task.FromResult(Result<Unit>.Ok(Unit.Value)), ct);
                return Result<Unit>.Ok(Unit.Value);
            }, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Nested", StringComparison.Ordinal))
        {
            nestedThrew = true;
        }

        var next = writer.ExecuteAsync<Unit>("Torture.AfterNested", (_, _) => Task.FromResult(Result<Unit>.Ok(Unit.Value)), ct);
        var released = await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(5), ct)) == next;
        report.Check("Torture: a nested ExecuteAsync throws instead of deadlocking, and the gate is released",
            nestedThrew && released);
    }

    private static async Task ConcurrentCommandsAreSerializedAsync(LeagueSession session, SaveWriter writer, Report report, CancellationToken ct)
    {
        var season0 = await Probe.ScalarAsync(session, "SELECT CurrentSeason FROM SaveInfo", ct);
        var generation0 = await Probe.ScalarAsync(session, "SELECT Generation FROM SaveInfo", ct);

        var commands = Enumerable.Range(0, 50).Select(_ => Task.Run(() => writer.ExecuteAsync<Unit>("Torture.Increment", async (uow, ct) =>
        {
            var info = await uow.Db.SaveInfo.SingleAsync(ct);
            info.CurrentSeason += 1;   // read-modify-write: overlapping commands would lose updates
            return Result<Unit>.Ok(Unit.Value);
        }, ct), ct)).ToList();
        await Task.WhenAll(commands);

        var season1 = await Probe.ScalarAsync(session, "SELECT CurrentSeason FROM SaveInfo", ct);
        var generation1 = await Probe.ScalarAsync(session, "SELECT Generation FROM SaveInfo", ct);
        report.Check("Torture: 50 concurrent read-modify-write commands are serialized (no lost updates)",
            season1 == season0 + 50 && generation1 == generation0 + 50,
            $"season +{season1 - season0}, generation +{generation1 - generation0}");

        await writer.ExecuteAsync<Unit>("Torture.Restore", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).CurrentSeason = (int)season0;
            return Result<Unit>.Ok(Unit.Value);
        }, ct);
    }

    private static async Task ReadDuringOpenWriteIsNotBlockedAsync(SaveWriter writer, SaveReader reader, Report report, CancellationToken ct)
    {
        var original = await reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.LeagueName).SingleAsync(ct), ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holder = writer.ExecuteAsync<Unit>("Torture.HoldOpenAfterWriting", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = original + " (renamed)";
            await uow.Db.SaveChangesAsync(ct);   // written inside the still-open transaction
            entered.SetResult();
#pragma warning disable VSTHRD003 // Deliberate: the test parks the command until it releases it.
            await release.Task;
#pragma warning restore VSTHRD003
            return Result<Unit>.Ok(Unit.Value);
        }, ct);

        await entered.Task;
        var stopwatch = Stopwatch.StartNew();
        var seenDuringWrite = await reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.LeagueName).SingleAsync(ct), ct);
        stopwatch.Stop();
        release.SetResult();
        await holder;
        var seenAfterCommit = await reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.LeagueName).SingleAsync(ct), ct);

        report.Check("Torture: a read during an open write isn't blocked and sees the last committed state (WAL)",
            seenDuringWrite == original && seenAfterCommit == original + " (renamed)" && stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"read took {Format.Ms(stopwatch.Elapsed)}");

        await writer.ExecuteAsync<Unit>("Torture.RestoreName", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = original;
            return Result<Unit>.Ok(Unit.Value);
        }, ct);
    }

    /// <summary>
    /// ARCHITECTURE.md §6.3 says SaveWriter begins an IMMEDIATE transaction. Hold a command open WITHOUT writing and see
    /// whether a second writer can still start: only an IMMEDIATE transaction already holds the write lock.
    /// </summary>
    private static async Task TransactionTakesTheWriteLockAtBeginAsync(LeagueSession session, SaveWriter writer, Report report, CancellationToken ct)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holder = writer.ExecuteAsync<Unit>("Torture.HoldOpenWithoutWriting", async (_, _) =>
        {
            entered.SetResult();
#pragma warning disable VSTHRD003 // Deliberate: the test parks the command until it releases it.
            await release.Task;
#pragma warning restore VSTHRD003
            return Result<Unit>.Ok(Unit.Value);
        }, ct);
        await entered.Task;

        var secondWriterBlocked = false;
        var probeConnectionString = new SqliteConnectionStringBuilder(session.ConnectionString) { DefaultTimeout = 1, Pooling = false }.ToString();
        await using (var probe = new SqliteConnection(probeConnectionString))
        {
            await probe.OpenAsync(ct);
            await using var command = probe.CreateCommand();
            command.CommandText = "BEGIN IMMEDIATE;";   // a probe of the lock state, not application code
            try
            {
                await command.ExecuteNonQueryAsync(ct);
                command.CommandText = "ROLLBACK;";
                await command.ExecuteNonQueryAsync(ct);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)   // SQLITE_BUSY
            {
                secondWriterBlocked = true;
            }
        }

        release.SetResult();
        await holder;
        report.Check("Torture: SaveWriter's transaction holds the write lock from BEGIN (IMMEDIATE), not from its first write",
            secondWriterBlocked,
            secondWriterBlocked ? "a second writer got SQLITE_BUSY" : "a second writer could start, so the transaction is DEFERRED");
    }

    private static async Task<(long Players, long Log, long Generation)> CountsAsync(LeagueSession session, CancellationToken ct) => (
        await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM Players", ct),
        await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM TransactionLog", ct),
        await Probe.ScalarAsync(session, "SELECT Generation FROM SaveInfo", ct));

    private static Player ProbePlayer() => new()
    {
        Status = PlayerStatus.FreeAgent, FirstName = "Probe", LastName = "Player", Position = "QB", Overall = 50,
    };
}
