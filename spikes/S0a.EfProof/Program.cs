// Spike S0a (M0 WP2): prove EF Core + the GFO unit-of-work rules before WP6 builds the real persistence layer.
// Run in Release from the repo root:  dotnet run -c Release --project spikes/S0a.EfProof
using System.Diagnostics;
using GridironFrontOffice.Spikes.S0a.EfProof.Data;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using GridironFrontOffice.Spikes.S0a.EfProof.Proofs;
using Microsoft.EntityFrameworkCore;

var ct = CancellationToken.None;
var directory = Path.Combine(Path.GetTempPath(), "gfo-spike-s0a", Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(directory);
var report = new Report();
var rng = new Random(42);
Console.WriteLine($"Spike S0a: EF Core proof. Working in {directory}");
Console.WriteLine();

// ---------------------------------------------------------------- create + seed
var path = Path.Combine(directory, "league.gfo");
var stopwatch = Stopwatch.StartNew();
await using (var modelProbe = new LeagueDbContext(LeagueSession.For(Path.Combine(directory, "model-probe.gfo")).Options))
{
    _ = modelProbe.Model;   // forces the one-time EF model build
}

report.Measure("EF model build: first context in the process (cold, one-time)", Format.Ms(stopwatch.Elapsed));

stopwatch.Restart();
var session = await SaveFile.CreateAsync(path, targetMigration: null, ct);
report.Measure("Create a save: WAL + both migrations (cold process, model already built)", Format.Ms(stopwatch.Elapsed));

using var writer = new SaveWriter(session);
var reader = new SaveReader(session);

stopwatch.Restart();
var seeded = await LeagueOps.SeedAsync(writer, rng, ct);
report.Measure("Seed through EF change tracking, one command: 32 teams, 1,996 players, 1,696 contracts, 6,784 contract years", Format.Ms(stopwatch.Elapsed));
report.Check("Seed command commits", !seeded.IsFailure);
report.Check("Typed-ID keys are INTEGER PRIMARY KEY AUTOINCREMENT (D-015)",
    (await Probe.TextAsync(session, "SELECT sql FROM sqlite_master WHERE name = 'Players'", ct))?.Contains("AUTOINCREMENT", StringComparison.Ordinal) == true);
report.Check("The save is in WAL mode", await Probe.TextAsync(session, "PRAGMA journal_mode;", ct) == "wal");

// The same work again in a now-warm process separates one-time JIT/model costs from the steady state.
var warmPath = Path.Combine(directory, "warm.gfo");
stopwatch.Restart();
var warmSession = await SaveFile.CreateAsync(warmPath, targetMigration: null, ct);
report.Measure("Create a second save (warm process)", Format.Ms(stopwatch.Elapsed));
using (var warmWriter = new SaveWriter(warmSession))
{
    stopwatch.Restart();
    await LeagueOps.SeedAsync(warmWriter, new Random(43), ct);
    report.Measure("Seed the same league again through EF (warm process)", Format.Ms(stopwatch.Elapsed));
}

await SaveFile.CloseAsync(warmSession, ct);
stopwatch.Restart();
var reopened = LeagueSession.For(warmPath);
var reopenedPlayers = await new SaveReader(reopened).QueryAsync((db, ct) => db.Players.CountAsync(ct), ct);
report.Measure("Open an existing save and run its first query (warm process)", Format.Ms(stopwatch.Elapsed));
report.Check("A reopened save reads back what was committed", reopenedPlayers == 1996, $"{reopenedPlayers} players");
await SaveFile.CloseAsync(reopened, ct);

// ---------------------------------------------------------------- (a) sign a free agent
var team = new TeamId(1);
var freeAgent = await reader.QueryAsync((db, ct) =>
    db.Players.Where(p => p.Status == PlayerStatus.FreeAgent).OrderBy(p => p.Id).Select(p => p.Id).FirstAsync(ct), ct);

var beforeSign = await SigningCountsAsync();
var overCap = await LeagueOps.SignFreeAgentAsync(writer, freeAgent, team, 60_000_000, ct);
var afterFailedSign = await SigningCountsAsync();
report.Check("(a) An over-the-cap signing returns Result.Fail and persists nothing",
    overCap.Error?.Code == "OverCap" && afterFailedSign == beforeSign, overCap.Error?.Message ?? "no error");

stopwatch.Restart();
var signed = await LeagueOps.SignFreeAgentAsync(writer, freeAgent, team, 2_000_000, ct);
report.Measure("(a) Sign a free agent: player + contract + 3 years + log, with a mid-command flush", Format.Ms(stopwatch.Elapsed));
var afterSign = await SigningCountsAsync();
var signedPlayerTeam = await Probe.ScalarAsync(session, $"SELECT COALESCE(TeamId, 0) FROM Players WHERE Id = {freeAgent.Value}", ct);
report.Check("(a) A valid signing commits every table together, and the typed ContractId round-trips",
    !signed.IsFailure && signed.Value.Value > 0
    && afterSign == (beforeSign.Contracts + 1, beforeSign.Years + 3, beforeSign.Log + 1) && signedPlayerTeam == team.Value,
    $"contract #{signed.Value.Value}, counts {beforeSign} -> {afterSign}");

// ---------------------------------------------------------------- (b) persist games
var rosterPairs = await reader.QueryAsync((db, ct) =>
    db.Players.Where(p => p.TeamId != null && p.Status == PlayerStatus.Active).Select(p => new { p.TeamId, p.Id }).ToListAsync(ct), ct);
var rosters = rosterPairs.GroupBy(x => x.TeamId!.Value).ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());
var teams = rosters.Keys.ToList();

stopwatch.Restart();
var season = SyntheticData.Games(rng, LeagueOps.Season, 285, teams, rosters);
report.Measure("Simulator stand-in: 285 games of stat lines + Brotli play logs, built OUTSIDE any transaction", Format.Ms(stopwatch.Elapsed));
report.Measure("Play log per game (synthetic, 170 plays): raw JSON -> Brotli",
    $"{Format.Bytes((long)season.Average(g => g.PlayLogRawBytes))} -> {Format.Bytes((long)season.Average(g => g.PlayLog.Length))}");

for (var run = 1; run <= 3; run++)
{
    var week = SyntheticData.Games(rng, LeagueOps.Season, 16, teams, rosters);
    stopwatch.Restart();
    var persisted = await LeagueOps.PersistGamesAsync(writer, week, awaitEachRow: true, ct);
    report.Measure($"(b) Persist a WEEK, one command: 16 games, {Format.Number(persisted.Value)} stat rows, 16 play logs, standings (run {run})", Format.Ms(stopwatch.Elapsed));
}

stopwatch.Restart();
var seasonPersisted = await LeagueOps.PersistGamesAsync(writer, season, awaitEachRow: true, ct);
var seasonElapsed = stopwatch.Elapsed;
report.Measure($"(b) Persist a SEASON, one command: 285 games, {Format.Number(seasonPersisted.Value)} stat rows, 285 play logs, standings", Format.Ms(seasonElapsed));
report.Check("(b) A full season of stats persists in under 1 s (the S0a target)", seasonElapsed < TimeSpan.FromSeconds(1), Format.Ms(seasonElapsed));

var secondSeason = SyntheticData.Games(rng, LeagueOps.Season, 285, teams, rosters);
stopwatch.Restart();
await LeagueOps.PersistGamesAsync(writer, secondSeason, awaitEachRow: false, ct);
report.Measure("(b) Same season with SYNC row inserts (ExecuteNonQuery), for comparison", Format.Ms(stopwatch.Elapsed));

var standingsGames = await Probe.ScalarAsync(session, "SELECT SUM(Wins + Losses + Ties) FROM TeamSeasons", ct);
var statRows = await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM PlayerGameStats", ct);
report.Check("(b) EF standings updates and raw stat inserts landed in the same commits",
    standingsGames == 2 * (48 + 285 + 285) && statRows == 90 * (48 + 285 + 285),
    $"{Format.Number(standingsGames / 2)} games in standings, {Format.Number(statRows)} stat rows");

await Probe.ScalarAsync(session, "PRAGMA wal_checkpoint(TRUNCATE);", ct);
report.Measure("Save size with the league + 618 games of stats and play logs (about 2.2 seasons)", Format.Bytes(Probe.FileSize(path)));

stopwatch.Restart();
var integrity = await Probe.TextAsync(session, "PRAGMA quick_check;", ct);
report.Measure($"PRAGMA quick_check on that {Format.Bytes(Probe.FileSize(path))} save (runs on every open)", Format.Ms(stopwatch.Elapsed));
report.Check("quick_check reports the save is intact", integrity == "ok", integrity ?? "");

// ---------------------------------------------------------------- (c) roster read model
var rosterSql = await reader.QueryAsync((db, _) => Task.FromResult(LeagueOps.RosterQuery(db, team).ToQueryString()), ct);
var roster = await LeagueOps.GetRosterAsync(reader, team, ct);
report.Check("(c) The roster read model runs as ONE SQL statement and projects 54 rows with cap hits",
    roster.Count == 54 && roster.TrueForAll(r => r.CapHit > 0) && !rosterSql.Contains(';', StringComparison.Ordinal),
    $"{roster.Count} rows");
var readTimes = new List<double>();
for (var i = 0; i < 205; i++)
{
    stopwatch.Restart();
    await LeagueOps.GetRosterAsync(reader, team, ct);
    readTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
}

report.Measure("(c) Roster read model (one team), avg / p95 over 200 runs after warm-up", Percentiles(readTimes.Skip(5).ToList()));
Console.WriteLine();
Console.WriteLine("(c) Generated SQL:");
Console.WriteLine(rosterSql);
Console.WriteLine();

// ---------------------------------------------------------------- command latency (the live draft writes about once per second)
var commandTimes = new List<double>();
for (var i = 0; i < 505; i++)
{
    stopwatch.Restart();
    await writer.ExecuteAsync<Unit>("Spike.Heartbeat", (_, _) => Task.FromResult(Result<Unit>.Ok(Unit.Value)), ct);
    commandTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
}

var commands = commandTimes.Skip(5).ToList();
report.Measure("Smallest real command (context + IMMEDIATE transaction + generation bump + commit), avg / p95 over 500", Percentiles(commands));
report.Check("A 1-per-second clock heartbeat is cheap: p95 command under 10 ms", P(commands, 0.95) < 10, Format.Ms(P(commands, 0.95)));

// ---------------------------------------------------------------- torture checks
await TortureChecks.RunAsync(session, writer, reader, report, ct);

// ---------------------------------------------------------------- close releases the file
await SaveFile.CloseAsync(session, ct);
var released = TryDelete(path, out var deleteError);
report.Check("Closing (checkpoint + ClearPool) releases the file: delete succeeds and no -wal/-shm remain", released, deleteError);

// ---------------------------------------------------------------- (d) migration on a populated save
await MigrationProof.RunAsync(directory, report, ct);

// ---------------------------------------------------------------- report
var reportPath = Path.Combine(directory, "S0a-results.md");
await File.WriteAllTextAsync(reportPath, report.ToMarkdown() + Environment.NewLine + "## Roster read model SQL" + Environment.NewLine + Environment.NewLine
    + "```sql" + Environment.NewLine + rosterSql + Environment.NewLine + "```" + Environment.NewLine, ct);
Console.WriteLine();
Console.WriteLine(report.AllPassed ? "ALL CHECKS PASSED" : "SOME CHECKS FAILED");
Console.WriteLine($"Report: {reportPath}");
return report.AllPassed ? 0 : 1;

// ---------------------------------------------------------------- helpers
async Task<(long Contracts, long Years, long Log)> SigningCountsAsync() => (
    await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM Contracts", ct),
    await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM ContractYears", ct),
    await Probe.ScalarAsync(session, "SELECT COUNT(*) FROM TransactionLog", ct));

static double P(List<double> samples, double percentile)
{
    var sorted = samples.Order().ToList();
    return sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(percentile * sorted.Count) - 1)];
}

static string Percentiles(List<double> samples) => $"{Format.Ms(samples.Average())} / {Format.Ms(P(samples, 0.95))}";

static bool TryDelete(string file, out string error)
{
    try
    {
        File.Delete(file);
        error = File.Exists(file + "-wal") || File.Exists(file + "-shm") ? "side files remain" : "";
        return !File.Exists(file) && error.Length == 0;
    }
    catch (IOException ex)
    {
        error = ex.Message;
        return false;
    }
}
