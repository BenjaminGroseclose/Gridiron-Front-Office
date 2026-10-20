using System.Data.Common;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Application.Data;

// The data-access contracts (DATA_ACCESS_GUIDE.md §3.2). Reads go through ISaveReader, writes through
// ISaveWriter.ExecuteAsync. Nothing else creates a DbContext, opens a connection or starts a transaction.

/// <summary>Tracked access to the league, only ever handed out inside a command.</summary>
public interface ILeagueDb
{
    DbSet<SaveInfo> SaveInfo { get; }

    /// <summary>An optional mid-command flush (e.g. to get generated IDs). It never commits: SaveWriter does.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>No-tracking queryables for screens and AI snapshots. Project to DTOs; never hand entities to the UI.</summary>
public interface ILeagueReadDb
{
    IQueryable<SaveInfo> SaveInfo { get; }
}

/// <summary>What a command receives: one transaction, from start to commit or rollback.</summary>
public interface IUnitOfWork
{
    /// <summary>Bound to this command's transaction.</summary>
    ILeagueDb Db { get; }

    /// <summary>Raw SQL on the same connection and transaction, for bulk paths (DATA_ACCESS_GUIDE.md §5.4).</summary>
    DbCommand CreateCommand(string sql);

    /// <summary>Delivered to subscribers only if the command commits.</summary>
    void Raise(IDomainEvent domainEvent);
}

/// <summary>The only way to change a save. One call = one command = one transaction.</summary>
public interface ISaveWriter
{
    /// <param name="operation">A name for the log, e.g. "FreeAgency.Sign".</param>
    /// <param name="work">
    /// The command. Return <see cref="Result.Fail{T}"/> for an expected failure; anything it changed is rolled back.
    /// Never call ExecuteAsync from inside it (that throws): share logic as operations that take the IUnitOfWork.
    /// </param>
    Task<Result<T>> ExecuteAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work,
        CancellationToken cancellationToken = default);
}

/// <summary>Read-only queries on a fresh no-tracking context. They run alongside a write and see the last commit.</summary>
public interface ISaveReader
{
    Task<T> QueryAsync<T>(Func<ILeagueReadDb, CancellationToken, Task<T>> query,
        CancellationToken cancellationToken = default);
}

/// <summary>Something that happened in a committed command, e.g. PlayerSigned. UI stores refresh from these.</summary>
public interface IDomainEvent;

/// <summary>
/// The in-house event hub (D-020): subscribers hear about events after the command that raised them commits,
/// on the thread that ran the command. Dispose the subscription to stop.
/// </summary>
public interface IDomainEventDispatcher
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent;
}
