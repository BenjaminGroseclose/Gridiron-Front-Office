using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using GridironFrontOffice.Spikes.S0a.EfProof.Data;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

/// <summary>
/// Proof (d): a save created by an "older app" (schema at the Initial migration), populated with a season of stats,
/// is reopened by the "newer app": pre-upgrade snapshot, then migrate on open, adding a column and backfilling it.
/// </summary>
internal static class MigrationProof
{
    private const string HasCareerColumnSql = "SELECT COUNT(*) FROM pragma_table_info('Players') WHERE name = 'CareerPassYards'";

    public static async Task RunAsync(string directory, Report report, CancellationToken ct)
    {
        var path = Path.Combine(directory, "old-save.gfo");
        var oldApp = await SaveFile.CreateAsync(path, "Initial", ct);
        report.Check("(d) An 'old' save is created at the Initial schema (no CareerPassYards column)",
            await Probe.ScalarAsync(oldApp, HasCareerColumnSql, ct) == 0);

        var populate = Stopwatch.StartNew();
        using (var writer = new SaveWriter(oldApp))
        {
            // Raw SQL only: the EF model already has the new column, the old schema doesn't.
            await writer.ExecuteAsync<Unit>("OldApp.Populate", async (uow, ct) =>
            {
                await ExecAsync(uow, "INSERT INTO SaveInfo (Id, LeagueName, CurrentSeason, Generation) VALUES (1, 'Old Save', 2027, 0)", ct);
                await RepeatAsync(uow, "INSERT INTO Teams (Abbreviation, Nickname) VALUES ($a, 'Old Team')", 32,
                    (p, i) => p["$a"].Value = "O" + i.ToString("00", CultureInfo.InvariantCulture), ct);
                await RepeatAsync(uow, "INSERT INTO Players (TeamId, Status, FirstName, LastName, Position, Overall, IsInjured) VALUES ($t, 0, 'Old', 'Player', 'QB', 70, 0)", 2000,
                    (p, i) => p["$t"].Value = 1 + (i % 32), ct);
                await RepeatAsync(uow, "INSERT INTO Games (Season, Week, HomeTeamId, AwayTeamId, HomeScore, AwayScore) VALUES (2027, $w, 1, 2, 21, 17)", 285,
                    (p, i) => p["$w"].Value = 1 + (i / 16), ct);

                var statColumns = LeagueDbContext.StatColumns;
                var statSql = "INSERT INTO PlayerGameStats (GameId, PlayerId, TeamId, " + string.Join(", ", statColumns) + ") VALUES ($g, $p, 1, $yards, " +
                              string.Join(", ", statColumns.Skip(1).Select(_ => "0")) + ")";
                await RepeatAsync(uow, statSql, 285 * 90, (p, i) =>
                {
                    var game = i / 90;
                    p["$g"].Value = game + 1;
                    p["$p"].Value = ((game * 90) + (i % 90)) % 2000 + 1;   // 90 distinct players per game
                    p["$yards"].Value = (i * 37) % 300;
                }, ct);
                return Result<Unit>.Ok(Unit.Value);
            }, ct);
        }

        populate.Stop();
        report.Measure("(d) Populate the old save: 2,000 players, 285 games, 25,650 stat rows (raw SQL, one command)", Format.Ms(populate.Elapsed));
        await SaveFile.CloseAsync(oldApp, ct);   // the old app exits

        // The new app opens it: snapshot first, then migrate (PERSISTENCE.md §7).
        var newApp = LeagueSession.For(path);
        var snapshotPath = Path.Combine(directory, "old-save.pre-upgrade-v1.gfo");
        var snapshot = Stopwatch.StartNew();
        await SaveFile.VacuumIntoAsync(newApp, snapshotPath, ct);
        snapshot.Stop();

        var migrate = Stopwatch.StartNew();
        await SaveFile.MigrateToLatestAsync(newApp, ct);
        migrate.Stop();

        report.Measure($"(d) Pre-upgrade snapshot with VACUUM INTO ({Format.Bytes(new FileInfo(snapshotPath).Length)})", Format.Ms(snapshot.Elapsed));
        report.Measure("(d) Migrate on open: add a column + backfill it from 25,650 stat rows", Format.Ms(migrate.Elapsed));

        var mismatches = await Probe.ScalarAsync(newApp,
            "SELECT COUNT(*) FROM Players p WHERE p.CareerPassYards <> (SELECT COALESCE(SUM(s.Stat01), 0) FROM PlayerGameStats s WHERE s.PlayerId = p.Id)", ct);
        var rawTotal = await Probe.ScalarAsync(newApp, "SELECT SUM(CareerPassYards) FROM Players", ct);
        var efTotal = await new SaveReader(newApp).QueryAsync((db, ct) => db.Players.SumAsync(p => p.CareerPassYards, ct), ct);
        var history = await Probe.ScalarAsync(newApp, "SELECT COUNT(*) FROM __EFMigrationsHistory", ct);

        report.Check("(d) Migrating on open adds the column and backfills every row correctly",
            await Probe.ScalarAsync(newApp, HasCareerColumnSql, ct) == 1 && mismatches == 0 && rawTotal > 0,
            $"{Format.Number(rawTotal)} total yards backfilled, {mismatches} mismatches");
        report.Check("(d) EF reads the migrated save through the current model", efTotal == rawTotal && history == 2,
            $"EF sum {Format.Number(efTotal)}, {history} migrations recorded");

        var snapshotSession = LeagueSession.For(snapshotPath);
        report.Check("(d) The pre-upgrade snapshot still holds the old schema (a safe rollback point)",
            await Probe.ScalarAsync(snapshotSession, HasCareerColumnSql, ct) == 0);

        await SaveFile.CloseAsync(snapshotSession, ct);
        await SaveFile.CloseAsync(newApp, ct);
    }

    private static async Task ExecAsync(IUnitOfWork uow, string sql, CancellationToken ct)
    {
        await using var command = uow.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>One prepared statement, executed <paramref name="count"/> times with parameters set by <paramref name="bind"/>.</summary>
    private static async Task RepeatAsync(IUnitOfWork uow, string sql, int count, Action<DbParameterCollection, int> bind, CancellationToken ct)
    {
        await using var command = uow.CreateCommand(sql);
        foreach (var name in sql.Split([' ', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries).Where(t => t.StartsWith('$')).Distinct())
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
        }

        await command.PrepareAsync(ct);
        for (var i = 0; i < count; i++)
        {
            bind(command.Parameters, i);
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
