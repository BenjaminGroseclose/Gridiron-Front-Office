namespace GridironFrontOffice.Spikes.S0a.EfProof.Domain;

// A small, domain-shaped model: enough tables to exercise multi-table commands, bulk paths and read models.

internal sealed class SaveInfo
{
    public int Id { get; set; } = 1;
    public string LeagueName { get; set; } = "";
    public int CurrentSeason { get; set; }
    public long Generation { get; set; }
}

internal sealed class Team
{
    public TeamId Id { get; set; }
    public string Abbreviation { get; set; } = "";
    public string Nickname { get; set; } = "";
}

internal enum PlayerStatus
{
    Active = 0,
    FreeAgent = 1,
}

internal sealed class Player
{
    public PlayerId Id { get; set; }
    public TeamId? TeamId { get; set; }
    public PlayerStatus Status { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public int Overall { get; set; }
    public bool IsInjured { get; set; }

    /// <summary>Added by the second migration and backfilled from existing stats (proof d).</summary>
    public long CareerPassYards { get; set; }

    public List<Contract> Contracts { get; } = [];
}

internal sealed class Contract
{
    public ContractId Id { get; set; }
    public PlayerId PlayerId { get; set; }
    public TeamId TeamId { get; set; }
    public bool IsActive { get; set; }
    public long TotalValue { get; set; }
    public List<ContractYear> Years { get; } = [];
}

internal sealed class ContractYear
{
    public int Id { get; set; }
    public ContractId ContractId { get; set; }
    public Contract? Contract { get; set; }
    public int Season { get; set; }
    public long BaseSalary { get; set; }
    public long ProratedBonus { get; set; }
    public long CapHit { get; set; }
}

internal sealed class TransactionLogEntry
{
    public int Id { get; set; }
    public int Season { get; set; }
    public string Kind { get; set; } = "";
    public TeamId TeamId { get; set; }
    public PlayerId PlayerId { get; set; }
}

internal sealed class Game
{
    public GameId Id { get; set; }
    public int Season { get; set; }
    public int Week { get; set; }
    public TeamId HomeTeamId { get; set; }
    public TeamId AwayTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}

/// <summary>Wide and sparse like the real table: 60 stat columns (shadow properties, see LeagueDbContext.StatColumns).</summary>
internal sealed class PlayerGameStats
{
    public GameId GameId { get; set; }
    public PlayerId PlayerId { get; set; }
    public TeamId TeamId { get; set; }
}

/// <summary>One Brotli-compressed JSON play log per game (PERSISTENCE.md §5.4).</summary>
internal sealed class GamePlayLog
{
    public GameId GameId { get; set; }
    public int EngineVersion { get; set; }
    public string Format { get; set; } = "";
    public string Compression { get; set; } = "";
    public byte[] Data { get; set; } = [];
}

internal sealed class TeamSeason
{
    public int Id { get; set; }
    public TeamId TeamId { get; set; }
    public int Season { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
}
