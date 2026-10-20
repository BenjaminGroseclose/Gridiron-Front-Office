using System.Data.Common;
using System.Diagnostics;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The ONLY transaction owner (DATA_ACCESS_GUIDE.md §3.4). One command = one DbContext = one connection = one
/// transaction. Commands run one at a time; Microsoft.Data.Sqlite begins them IMMEDIATE, so the write lock is held
/// from BEGIN (proven in Spike S0a). Events raised by a command are delivered after it commits and the gate is free.
/// </summary>
internal sealed class SaveWriter(LeagueSessionAccessor sessions, DomainEventDispatcher events, ILogger<SaveWriter> log)
    : ISaveWriter, IDisposable
{
    /// <summary>A command slower than this is logged as a Warning (LOGGING.md §8, "slow operations").</summary>
    public const long SlowCommandMs = 1000;

    private static readonly AsyncLocal<bool> InCommand = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<Result<T>> ExecuteAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(work);
        ThrowIfNested(operation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Result<T> result;
        IReadOnlyList<IDomainEvent> raised;
        InCommand.Value = true;
        try
        {
            (result, raised) = await RunAsync(operation, work, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            InCommand.Value = false;
            _gate.Release();
        }

        // After the commit and outside the gate, so a subscriber may start a new command.
        events.Dispatch(raised);
        return result;
    }

    /// <summary>
    /// For SaveManager and SnapshotService: runs <paramref name="action"/> while no command can run (waiting for the
    /// current one to finish), e.g. to swap or release the session, or to take a snapshot between commands.
    /// </summary>
    public async Task RunExclusiveAsync(string operation, Func<Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfNested(operation);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<(Result<T> Result, IReadOnlyList<IDomainEvent> Raised)> RunAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var db = new LeagueDbContext(sessions.Current.Options);
        await using (db.ConfigureAwait(false))
        {
#pragma warning disable RS0030 // Banned API: SaveWriter is the one place allowed to begin a transaction (DATA_ACCESS_GUIDE.md §8).
            var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
#pragma warning restore RS0030
            await using (transaction.ConfigureAwait(false))
            {
                var uow = new UnitOfWork(db);
                Result<T> result;
                try
                {
                    result = await work(uow, ct).ConfigureAwait(false);
                    if (result.IsFailure)
                    {
                        // An expected business failure: nothing it changed is kept.
                        await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                        var error = result.Error!.ToString();
                        DataLog.Rejected(log, operation, error);
                        return (result, []);
                    }

                    await db.SaveChangesAsync(ct).ConfigureAwait(false);
                    await db.Database.ExecuteSqlRawAsync("UPDATE SaveInfo SET Generation = Generation + 1", ct).ConfigureAwait(false);
                    // The only commit in the codebase. CancellationToken.None: once the work is done, a cancellation
                    // must not leave the transaction half-handled.
                    await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await TryRollbackAsync(transaction, operation).ConfigureAwait(false);
                    DataLog.RolledBack(log, ex, operation);
                    throw;
                }

                var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if (elapsedMs > SlowCommandMs)
                {
                    DataLog.Slow(log, operation, elapsedMs, SlowCommandMs);
                }
                else
                {
                    DataLog.Committed(log, operation, elapsedMs);
                }

                return (result, uow.RaisedEvents);
            }
        }
    }

    private async Task TryRollbackAsync(IDbContextTransaction transaction, string operation)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackFailure) when (rollbackFailure is DbException or InvalidOperationException)
        {
            // Keep the original exception; the transaction dies with its connection anyway.
            DataLog.RollbackFailed(log, rollbackFailure, operation);
        }
    }

    private static void ThrowIfNested(string operation)
    {
        if (InCommand.Value)
        {
            throw new InvalidOperationException(
                $"Nested ISaveWriter.ExecuteAsync ('{operation}'). A command can't start another command; " +
                "share the logic as an operation that takes the existing IUnitOfWork (DATA_ACCESS_GUIDE.md §5.3).");
        }
    }
}

/// <summary>What a command receives (DATA_ACCESS_GUIDE.md §3.5).</summary>
internal sealed class UnitOfWork(LeagueDbContext db) : IUnitOfWork
{
    private readonly List<IDomainEvent> _raised = [];

    public ILeagueDb Db => db;

    public IReadOnlyList<IDomainEvent> RaisedEvents => _raised;

    public void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _raised.Add(domainEvent);
    }

    public DbCommand CreateCommand(string sql)
    {
        var command = (SqliteCommand)db.Database.GetDbConnection().CreateCommand();
        command.Transaction = (SqliteTransaction)db.Database.CurrentTransaction!.GetDbTransaction();   // same transaction, always
        command.CommandText = sql;
        return command;
    }
}

/// <summary>Read-only queries on a fresh no-tracking context (DATA_ACCESS_GUIDE.md §3.5). WAL: never blocks the writer.</summary>
internal sealed class SaveReader(LeagueSessionAccessor sessions) : ISaveReader
{
    public async Task<T> QueryAsync<T>(Func<ILeagueReadDb, CancellationToken, Task<T>> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var db = new LeagueDbContext(sessions.Current.Options);
        await using (db.ConfigureAwait(false))
        {
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return await query(db, cancellationToken).ConfigureAwait(false);
        }
    }
}
