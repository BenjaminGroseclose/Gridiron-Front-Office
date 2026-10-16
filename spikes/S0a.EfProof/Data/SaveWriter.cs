using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

/// <summary>
/// DATA_ACCESS_GUIDE.md §3.4: the ONLY transaction owner. One command = one DbContext = one connection = one transaction.
/// </summary>
internal sealed class SaveWriter(LeagueSession session) : ISaveWriter, IDisposable
{
    private static readonly AsyncLocal<bool> InCommand = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<Result<T>> ExecuteAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work, CancellationToken ct = default)
    {
        if (InCommand.Value)
        {
            throw new InvalidOperationException(
                $"Nested ISaveWriter.ExecuteAsync ('{operation}'). Compose with the existing IUnitOfWork instead.");
        }

        await _gate.WaitAsync(ct);
        InCommand.Value = true;
        try
        {
            await using var db = new LeagueDbContext(session.Options);
#pragma warning disable RS0030 // Banned API: SaveWriter is the one allowed place to begin transactions.
            await using var tx = await db.Database.BeginTransactionAsync(ct);
#pragma warning restore RS0030
            var uow = new UnitOfWork(db);
            try
            {
                var result = await work(uow, ct);
                if (result.IsFailure)
                {
                    await tx.RollbackAsync(CancellationToken.None);
                    return result;
                }

                await db.SaveChangesAsync(ct);
                await db.Database.ExecuteSqlRawAsync("UPDATE SaveInfo SET Generation = Generation + 1", ct);
                await tx.CommitAsync(CancellationToken.None);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        finally
        {
            InCommand.Value = false;
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}

/// <summary>DATA_ACCESS_GUIDE.md §3.5.</summary>
internal sealed class UnitOfWork(LeagueDbContext db) : IUnitOfWork
{
    private readonly List<IDomainEvent> _events = [];

    public ILeagueDb Db => db;
    public IReadOnlyList<IDomainEvent> RaisedEvents => _events;

    public void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public DbCommand CreateCommand(string sql)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();   // same transaction, always
        command.CommandText = sql;
        return command;
    }
}

/// <summary>DATA_ACCESS_GUIDE.md §3.5: a fresh no-tracking context per query, no transaction.</summary>
internal sealed class SaveReader(LeagueSession session) : ISaveReader
{
    public async Task<T> QueryAsync<T>(Func<ILeagueReadDb, CancellationToken, Task<T>> query, CancellationToken ct = default)
    {
        await using var db = new LeagueDbContext(session.Options);
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return await query(db, ct);
    }
}
