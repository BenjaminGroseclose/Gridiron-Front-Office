using System.Data.Common;
using System.Globalization;
using GridironFrontOffice.Spikes.S0a.EfProof.Data;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

internal sealed record RosterRow(PlayerId Id, string Name, string Position, int Overall, long CapHit, bool IsInjured);

/// <summary>The four representative operations from DATA_ACCESS_OPTIONS.md §5, written like real Application commands.</summary>
internal static class LeagueOps
{
    public const int Season = 2027;
    public const long SalaryCap = 255_000_000;
    public const int RosterSize = 53;
    public const int FreeAgents = 300;

    private static readonly string[] Positions = ["QB", "RB", "WR", "TE", "OT", "OG", "C", "EDGE", "DT", "LB", "CB", "S", "K", "P", "LS"];

    private static readonly string InsertStatsSql =
        "INSERT INTO PlayerGameStats (GameId, PlayerId, TeamId, " + string.Join(", ", LeagueDbContext.StatColumns) + ") " +
        "VALUES ($game, $player, $team, " + string.Join(", ", LeagueDbContext.StatColumns.Select((_, i) => "$s" + i.ToString(CultureInfo.InvariantCulture))) + ")";

    private const string InsertPlayLogSql =
        "INSERT INTO GamePlayLogs (GameId, EngineVersion, Format, Compression, Data) VALUES ($game, 1, 'json-v1', 'brotli', $data)";

    // ---------------------------------------------------------------- seed (also the M1 generation-time signal)

    /// <summary>32 teams × 53 players with 4-year contracts, plus free agents, in ONE command through EF change tracking.</summary>
    public static Task<Result<Unit>> SeedAsync(ISaveWriter writer, Random rng, CancellationToken ct) =>
        writer.ExecuteAsync("Spike.Seed", async (uow, ct) =>
        {
            uow.Db.SaveInfo.Add(new SaveInfo { LeagueName = "S0a Spike League", CurrentSeason = Season });

            var teams = Enumerable.Range(0, 32)
                .Select(i => new Team { Abbreviation = "T" + i.ToString("00", CultureInfo.InvariantCulture), Nickname = "Team " + i.ToString(CultureInfo.InvariantCulture) })
                .ToList();
            uow.Db.Teams.AddRange(teams);
            await uow.Db.SaveChangesAsync(ct);   // mid-command flush to get the generated team IDs (same transaction)

            foreach (var team in teams)
            {
                for (var slot = 0; slot < RosterSize; slot++)
                {
                    var player = NewPlayer(rng, team.Id, slot);
                    var capHit = CapHitFor(rng, slot);
                    var contract = new Contract { TeamId = team.Id, IsActive = true, TotalValue = capHit * 4 };
                    for (var year = 0; year < 4; year++)
                    {
                        contract.Years.Add(new ContractYear
                        {
                            Season = Season + year,
                            BaseSalary = capHit * 8 / 10,
                            ProratedBonus = capHit - (capHit * 8 / 10),
                            CapHit = capHit,
                        });
                    }

                    player.Contracts.Add(contract);
                    uow.Db.Players.Add(player);
                }

                uow.Db.TeamSeasons.Add(new TeamSeason { TeamId = team.Id, Season = Season });
            }

            for (var i = 0; i < FreeAgents; i++)
            {
                uow.Db.Players.Add(NewPlayer(rng, null, i % RosterSize));
            }

            return Result<Unit>.Ok(Unit.Value);
        }, ct);

    // ---------------------------------------------------------------- (a) multi-table command with a business rule

    /// <summary>DATA_ACCESS_GUIDE.md §5.2: all-or-nothing; over the cap returns Result.Fail and nothing persists.</summary>
    public static Task<Result<ContractId>> SignFreeAgentAsync(ISaveWriter writer, PlayerId playerId, TeamId teamId,
        long capHit, CancellationToken ct) =>
        writer.ExecuteAsync("FreeAgency.Sign", async (uow, ct) =>
        {
            var player = await uow.Db.Players.SingleAsync(p => p.Id == playerId, ct);
            if (player.Status != PlayerStatus.FreeAgent)
            {
                return Result<ContractId>.Fail(DomainError.NotAFreeAgent(playerId));
            }

            var committed = await uow.Db.ContractYears
                .Where(y => y.Season == Season && y.Contract!.IsActive && y.Contract.TeamId == teamId)
                .SumAsync(y => y.CapHit, ct);
            var overBy = committed + capHit - SalaryCap;
            if (overBy > 0)
            {
                return Result<ContractId>.Fail(DomainError.OverCap(teamId, overBy));
            }

            player.TeamId = teamId;
            player.Status = PlayerStatus.Active;
            var contract = new Contract { PlayerId = player.Id, TeamId = teamId, IsActive = true, TotalValue = capHit * 3 };
            uow.Db.Contracts.Add(contract);
            await uow.Db.SaveChangesAsync(ct);   // flush for contract.Id (guide §5.3)

            for (var year = 0; year < 3; year++)
            {
                uow.Db.ContractYears.Add(new ContractYear { ContractId = contract.Id, Season = Season + year, BaseSalary = capHit, CapHit = capHit });
            }

            uow.Db.TransactionLog.Add(new TransactionLogEntry { Season = Season, Kind = "Signing", TeamId = teamId, PlayerId = player.Id });
            return Result<ContractId>.Ok(contract.Id);
        }, ct);

    // ---------------------------------------------------------------- (b) bulk: EF for games + standings, raw SQL for stats and logs

    /// <param name="awaitEachRow">True awaits ExecuteNonQueryAsync per row (what the analyzers want); false uses the sync call, for comparison.</param>
    public static Task<Result<int>> PersistGamesAsync(ISaveWriter writer, IReadOnlyList<GameResult> results, bool awaitEachRow,
        CancellationToken ct) =>
        writer.ExecuteAsync("Season.PersistGames", async (uow, ct) =>
        {
            var games = results.Select(r => new Game
            {
                Season = r.Season, Week = r.Week, HomeTeamId = r.Home, AwayTeamId = r.Away, HomeScore = r.HomeScore, AwayScore = r.AwayScore,
            }).ToList();
            uow.Db.Games.AddRange(games);
            await uow.Db.SaveChangesAsync(ct);   // generated GameIds, needed by the raw inserts below

            var rows = 0;
            await using (var stats = uow.CreateCommand(InsertStatsSql))
            {
                var game = AddParameter(stats, "$game");
                var player = AddParameter(stats, "$player");
                var team = AddParameter(stats, "$team");
                var columns = LeagueDbContext.StatColumns.Select((_, i) => AddParameter(stats, "$s" + i.ToString(CultureInfo.InvariantCulture))).ToArray();
                await stats.PrepareAsync(ct);   // compile once, execute many

                for (var g = 0; g < results.Count; g++)
                {
                    foreach (var line in results[g].Lines)
                    {
                        game.Value = games[g].Id.Value;
                        player.Value = line.PlayerId.Value;
                        team.Value = line.TeamId.Value;
                        for (var s = 0; s < columns.Length; s++)
                        {
                            columns[s].Value = line.Stats[s];
                        }

                        if (awaitEachRow)
                        {
                            await stats.ExecuteNonQueryAsync(ct);
                        }
                        else
                        {
#pragma warning disable VSTHRD103 // Deliberate: measuring the sync path for comparison.
                            stats.ExecuteNonQuery();
#pragma warning restore VSTHRD103
                        }

                        rows++;
                    }
                }
            }

            await using (var logs = uow.CreateCommand(InsertPlayLogSql))
            {
                var game = AddParameter(logs, "$game");
                var data = AddParameter(logs, "$data");
                await logs.PrepareAsync(ct);
                for (var g = 0; g < results.Count; g++)
                {
                    game.Value = games[g].Id.Value;
                    data.Value = results[g].PlayLog;
                    await logs.ExecuteNonQueryAsync(ct);
                }
            }

            // Standings through EF change tracking, in the same transaction as the raw inserts.
            var standings = await uow.Db.TeamSeasons.Where(s => s.Season == Season).ToDictionaryAsync(s => s.TeamId, ct);
            foreach (var r in results)
            {
                Apply(standings[r.Home], r.HomeScore, r.AwayScore);
                Apply(standings[r.Away], r.AwayScore, r.HomeScore);
            }

            return Result<int>.Ok(rows);
        }, ct);

    // ---------------------------------------------------------------- (c) read model

    /// <summary>DATA_ACCESS_GUIDE.md §4: players + current cap hit + injury status, projected to a DTO in one query.</summary>
    public static IQueryable<RosterRow> RosterQuery(ILeagueReadDb db, TeamId teamId) =>
        db.Players
            .Where(p => p.TeamId == teamId && p.Status == PlayerStatus.Active)
            .OrderBy(p => p.Position).ThenByDescending(p => p.Overall)
            .Select(p => new RosterRow(
                p.Id,
                p.FirstName + " " + p.LastName,
                p.Position,
                p.Overall,
                p.Contracts.Where(c => c.IsActive).SelectMany(c => c.Years).Where(y => y.Season == Season).Select(y => y.CapHit).FirstOrDefault(),
                p.IsInjured));

    public static Task<List<RosterRow>> GetRosterAsync(ISaveReader reader, TeamId teamId, CancellationToken ct) =>
        reader.QueryAsync((db, ct) => RosterQuery(db, teamId).ToListAsync(ct), ct);

    // ---------------------------------------------------------------- helpers

    private static void Apply(TeamSeason row, int pointsFor, int pointsAgainst)
    {
        row.PointsFor += pointsFor;
        row.PointsAgainst += pointsAgainst;
        if (pointsFor > pointsAgainst)
        {
            row.Wins++;
        }
        else if (pointsFor < pointsAgainst)
        {
            row.Losses++;
        }
        else
        {
            row.Ties++;
        }
    }

    private static DbParameter AddParameter(DbCommand command, string name)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        command.Parameters.Add(parameter);
        return parameter;
    }

    private static Player NewPlayer(Random rng, TeamId? teamId, int slot) => new()
    {
        TeamId = teamId,
        Status = teamId is null ? PlayerStatus.FreeAgent : PlayerStatus.Active,
        FirstName = "First" + rng.Next(1000).ToString(CultureInfo.InvariantCulture),
        LastName = "Last" + rng.Next(5000).ToString(CultureInfo.InvariantCulture),
        Position = Positions[slot % Positions.Length],
        Overall = rng.Next(45, 95),
        IsInjured = rng.Next(20) == 0,
    };

    // About $228M of the $255M cap per team: a few stars, a middle class, and minimum deals.
    private static long CapHitFor(Random rng, int slot) => slot switch
    {
        < 5 => rng.Next(18_000_000, 30_000_000),
        < 15 => rng.Next(3_000_000, 9_000_000),
        _ => rng.Next(900_000, 1_600_000),
    };
}
