using System.Diagnostics;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

/// <summary>
/// DATA_ACCESS_GUIDE.md §9: the guarantees the whole game rests on, proven on every build. The domain doesn't exist
/// yet, so the tests use SaveInfo plus a test-only Probe table created with raw SQL.
/// </summary>
public sealed class TransactionTortureTests : IAsyncLifetime
{
    private TestSave _save = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _save = await TestSave.CreateAsync();
        await _save.CreateProbeTableAsync();
    }

    public ValueTask DisposeAsync() => _save.DisposeAsync();

    [Fact]
    public async Task An_exception_midway_persists_nothing()
    {
        var generation = _save.Scalar("SELECT Generation FROM SaveInfo");

        await Should.ThrowAsync<InvalidOperationException>(() => _save.Writer.ExecuteAsync<Unit>("Torture.Throw", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = "Renamed";
            await uow.Db.SaveChangesAsync(ct);                    // flushed, not committed
            await InsertProbeAsync(uow, "raw", ct);               // raw SQL in the same transaction
            throw new InvalidOperationException("boom");
        }, Ct));

        _save.ScalarText("SELECT LeagueName FROM SaveInfo").ShouldBe("Torture League");
        _save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(0);
        _save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(generation);
    }

    [Fact]
    public async Task A_domain_failure_persists_nothing()
    {
        var generation = _save.Scalar("SELECT Generation FROM SaveInfo");

        var result = await _save.Writer.ExecuteAsync<Unit>("Torture.Fail", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = "Renamed";
            await uow.Db.SaveChangesAsync(ct);
            await InsertProbeAsync(uow, "raw", ct);
            return Result.Fail<Unit>(new DomainError("Rejected", "Over the cap"));
        }, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("Rejected");
        _save.ScalarText("SELECT LeagueName FROM SaveInfo").ShouldBe("Torture League");
        _save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(0);
        _save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(generation);
    }

    [Fact]
    public async Task EF_and_raw_SQL_in_one_command_commit_together()
    {
        var generation = _save.Scalar("SELECT Generation FROM SaveInfo");

        var result = await _save.Writer.ExecuteAsync<Unit>("Torture.Both", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = "Renamed";   // left for SaveWriter to flush at commit
            await InsertProbeAsync(uow, "raw", ct);
            return Result.Ok();
        }, Ct);

        result.IsSuccess.ShouldBeTrue();
        _save.ScalarText("SELECT LeagueName FROM SaveInfo").ShouldBe("Renamed");
        _save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(1);
        _save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(generation + 1);
    }

    [Fact]
    public async Task A_read_during_an_open_write_is_not_blocked_and_sees_the_last_commit()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holder = _save.Writer.ExecuteAsync<Unit>("Torture.HoldOpen", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = "Renamed";
            await uow.Db.SaveChangesAsync(ct);    // written inside the still-open transaction
            entered.SetResult();
#pragma warning disable VSTHRD003 // Deliberate: the test parks the command until it releases it.
            await release.Task;
#pragma warning restore VSTHRD003
            return Result.Ok();
        }, Ct);
        await entered.Task;

        var stopwatch = Stopwatch.StartNew();
        var duringWrite = await _save.Reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.LeagueName).SingleAsync(ct), Ct);
        stopwatch.Stop();
        release.SetResult();
        await holder;
        var afterCommit = await _save.Reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.LeagueName).SingleAsync(ct), Ct);

        duringWrite.ShouldBe("Torture League");
        afterCommit.ShouldBe("Renamed");
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task The_transaction_holds_the_write_lock_from_BEGIN()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holder = _save.Writer.ExecuteAsync<Unit>("Torture.HoldWithoutWriting", async (_, _) =>
        {
            entered.SetResult();
#pragma warning disable VSTHRD003 // Deliberate: the test parks the command until it releases it.
            await release.Task;
#pragma warning restore VSTHRD003
            return Result.Ok();
        }, Ct);
        await entered.Task;

        // Only an IMMEDIATE transaction holds the write lock before it has written anything.
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _save.Save.FilePath, Pooling = false, DefaultTimeout = 1 }.ToString();
        var secondWriter = Should.Throw<SqliteException>(() =>
        {
            using var probe = new SqliteConnection(connectionString);
            probe.Open();
            using var begin = probe.CreateCommand();
            begin.CommandText = "BEGIN IMMEDIATE;";   // a probe of the lock, not application code
            begin.ExecuteNonQuery();
        });

        release.SetResult();
        await holder;
        secondWriter.SqliteErrorCode.ShouldBe(5);   // SQLITE_BUSY
    }

    [Fact]
    public async Task A_nested_command_throws_instead_of_deadlocking_and_releases_the_gate()
    {
        var nested = await Should.ThrowAsync<InvalidOperationException>(() => _save.Writer.ExecuteAsync<Unit>("Torture.Outer", async (_, ct) =>
        {
            await _save.Writer.ExecuteAsync("Torture.Inner", (_, _) => Task.FromResult(Result.Ok()), ct);
            return Result.Ok();
        }, Ct));

        nested.Message.ShouldContain("Nested ISaveWriter.ExecuteAsync ('Torture.Inner')");
        var next = _save.Writer.ExecuteAsync("Torture.After", (_, _) => Task.FromResult(Result.Ok()), Ct);
        (await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))).ShouldBe(next);
    }

    [Fact]
    public async Task Fifty_concurrent_read_modify_write_commands_lose_no_update()
    {
        var generation = _save.Scalar("SELECT Generation FROM SaveInfo");

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => _save.Writer.ExecuteAsync<Unit>("Torture.Increment", async (uow, ct) =>
        {
            var info = await uow.Db.SaveInfo.SingleAsync(ct);
            info.PlayTimeSeconds += 1;   // read-modify-write: overlapping commands would lose updates
            return Result.Ok();
        }, Ct), Ct)));

        _save.Scalar("SELECT PlayTimeSeconds FROM SaveInfo").ShouldBe(50);
        _save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(generation + 50);
    }

    [Fact]
    public async Task Racing_versioned_commands_record_exactly_one()
    {
        // The live draft's pattern (DATA_ACCESS_GUIDE.md §5.5): each command carries the version it expects and checks
        // it inside the transaction. Generation stands in for the draft state's version.
        var expected = _save.Scalar("SELECT Generation FROM SaveInfo");

        Task<Result<Unit>> Pick(string who) => _save.Writer.ExecuteAsync<Unit>("Torture.Pick", async (uow, ct) =>
        {
            var info = await uow.Db.SaveInfo.SingleAsync(ct);
            if (info.Generation != expected)
            {
                return Result.Fail<Unit>(new DomainError("PickAlreadyMade", "The other command won the race."));
            }

            await InsertProbeAsync(uow, who, ct);
            return Result.Ok();
        }, Ct);

        var results = await Task.WhenAll(Pick("user"), Pick("clock"));

        results.Count(r => r.IsSuccess).ShouldBe(1);
        results.Single(r => r.IsFailure).Error!.Code.ShouldBe("PickAlreadyMade");
        _save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(1);
    }

    [Fact]
    public async Task Events_are_delivered_after_commit_only_and_a_subscriber_can_start_a_command()
    {
        var seen = new List<string>();
        var fromSubscriber = new List<Task<Result<Unit>>>();
        using var subscription = _save.Events.Subscribe<ProbeEvent>(e =>
        {
            seen.Add(e.Name);
            // A subscriber may start a new command: dispatch happens after the writer's gate is free again, so this
            // isn't "nested" (that check throws synchronously, right here, if it were).
            fromSubscriber.Add(_save.Writer.ExecuteAsync("Torture.FromSubscriber", (_, _) => Task.FromResult(Result.Ok()), Ct));
        });

        await _save.Writer.ExecuteAsync<Unit>("Torture.Raise", (uow, _) =>
        {
            uow.Raise(new ProbeEvent("committed"));
            return Task.FromResult(Result.Ok());
        }, Ct);
        await _save.Writer.ExecuteAsync<Unit>("Torture.RaiseThenFail", (uow, _) =>
        {
            uow.Raise(new ProbeEvent("rolled back"));
            return Task.FromResult(Result.Fail<Unit>(new DomainError("No", "rolled back")));
        }, Ct);

        seen.ShouldBe(["committed"]);
        (await fromSubscriber.ShouldHaveSingleItem()).IsSuccess.ShouldBeTrue();
    }

    private static async Task InsertProbeAsync(IUnitOfWork uow, string value, CancellationToken ct)
    {
        await using var insert = uow.CreateCommand("INSERT INTO Probe (Value) VALUES ($value);");
        var parameter = insert.CreateParameter();
        parameter.ParameterName = "$value";
        parameter.Value = value;
        insert.Parameters.Add(parameter);
        await insert.ExecuteNonQueryAsync(ct);
    }

    private sealed record ProbeEvent(string Name) : IDomainEvent;
}
