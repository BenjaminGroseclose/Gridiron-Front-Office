using System.Data.Common;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

// The Application-side contracts from DATA_ACCESS_GUIDE.md §3.2, as written there.

/// <summary>Tracked access, only ever handed out inside a command.</summary>
internal interface ILeagueDb
{
    DbSet<SaveInfo> SaveInfo { get; }
    DbSet<Team> Teams { get; }
    DbSet<Player> Players { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractYear> ContractYears { get; }
    DbSet<TransactionLogEntry> TransactionLog { get; }
    DbSet<Game> Games { get; }
    DbSet<TeamSeason> TeamSeasons { get; }

    /// <summary>Optional mid-command flush (e.g., to get generated IDs). Never commits.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>No-tracking queryables for screens and AI snapshots.</summary>
internal interface ILeagueReadDb
{
    IQueryable<SaveInfo> SaveInfo { get; }
    IQueryable<Team> Teams { get; }
    IQueryable<Player> Players { get; }
    IQueryable<Contract> Contracts { get; }
    IQueryable<TransactionLogEntry> TransactionLog { get; }
}

internal interface IDomainEvent;

internal interface IUnitOfWork
{
    /// <summary>Bound to THIS command's transaction.</summary>
    ILeagueDb Db { get; }

    /// <summary>Raw SQL on the SAME connection and transaction (hot paths only).</summary>
    DbCommand CreateCommand(string sql);

    /// <summary>Delivered only if the command commits.</summary>
    void Raise(IDomainEvent domainEvent);
}

internal interface ISaveWriter
{
    Task<Result<T>> ExecuteAsync<T>(string operation,
        Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work,
        CancellationToken ct = default);
}

internal interface ISaveReader
{
    Task<T> QueryAsync<T>(Func<ILeagueReadDb, CancellationToken, Task<T>> query,
        CancellationToken ct = default);
}

internal sealed record DomainError(string Code, string Message)
{
    public static DomainError OverCap(TeamId team, long overBy) =>
        new("OverCap", $"Team {team.Value} would be over the cap by {overBy:N0} dollars");

    public static DomainError NotAFreeAgent(PlayerId player) =>
        new("NotAFreeAgent", $"Player {player.Value} is not a free agent");

    public static DomainError Rejected(string why) => new("Rejected", why);
}

internal readonly record struct Unit
{
    public static Unit Value => default;
}

/// <summary>Expected business failures are results, not exceptions (DATA_ACCESS_GUIDE.md §3.2).</summary>
internal sealed class Result<T>
{
    private Result(T? value, DomainError? error) => (Value, Error) = (value, error);

    public T? Value { get; }
    public DomainError? Error { get; }
    public bool IsFailure => Error is not null;

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(DomainError error) => new(default, error);
}
