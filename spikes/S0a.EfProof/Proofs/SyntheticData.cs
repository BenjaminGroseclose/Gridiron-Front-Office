using System.IO.Compression;
using System.Text.Json;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

internal sealed record StatLine(PlayerId PlayerId, TeamId TeamId, int[] Stats);

internal sealed record GameResult(
    int Season, int Week, TeamId Home, TeamId Away, int HomeScore, int AwayScore,
    IReadOnlyList<StatLine> Lines, byte[] PlayLog, int PlayLogRawBytes);

/// <summary>A play event shaped like SIMULATION_ENGINE.md §2 (situation, call, outcome, participants, deciding matchup).</summary>
internal sealed record PlayEvent(
    int N, int Quarter, int Clock, int Down, int Distance, int YardLine, string Call, string Outcome, int Yards,
    int? Passer, int? Target, int? Carrier, int Tackler, string Matchup, int Winner, int Loser, float Margin);

/// <summary>
/// Stand-in for the simulator: produces game results OUTSIDE any transaction, as the real "Play Week" flow does
/// (DATA_ACCESS_GUIDE.md §5.6). The values are random, so play log compression is only an early estimate.
/// </summary>
internal static class SyntheticData
{
    public const int DressedPerTeam = 45;

    private static readonly string[] Calls = ["Run", "Pass", "PlayAction", "Screen", "Punt", "FieldGoal", "Kickoff"];
    private static readonly string[] Outcomes = ["Gain", "Loss", "Incomplete", "Complete", "Sack", "Touchdown", "Penalty", "Interception", "Fumble"];
    private static readonly string[] Matchups = ["PassRushVsBlock", "CoverageVsRoute", "RunBlockVsRunD", "CarrierVsTackler", "KickVsDistance"];

    public static List<GameResult> Games(Random rng, int season, int count,
        IReadOnlyList<TeamId> teams, IReadOnlyDictionary<TeamId, List<PlayerId>> rosters)
    {
        var games = new List<GameResult>(count);
        for (var i = 0; i < count; i++)
        {
            var home = teams[rng.Next(teams.Count)];
            TeamId away;
            do
            {
                away = teams[rng.Next(teams.Count)];
            }
            while (away == home);

            var lines = new List<StatLine>(DressedPerTeam * 2);
            lines.AddRange(rosters[home].Take(DressedPerTeam).Select(p => new StatLine(p, home, Stats(rng))));
            lines.AddRange(rosters[away].Take(DressedPerTeam).Select(p => new StatLine(p, away, Stats(rng))));

            var (playLog, rawBytes) = PlayLog(rng, lines);
            games.Add(new GameResult(season, (i / 16) + 1, home, away, rng.Next(0, 45), rng.Next(0, 45), lines, playLog, rawBytes));
        }

        return games;
    }

    // Sparse, like real stat lines: about 8 of 60 columns are non-zero.
    private static int[] Stats(Random rng)
    {
        var stats = new int[60];
        for (var k = 0; k < 8; k++)
        {
            stats[rng.Next(stats.Length)] = rng.Next(1, 120);
        }

        return stats;
    }

    private static (byte[] Compressed, int RawBytes) PlayLog(Random rng, List<StatLine> lines)
    {
        var events = new List<PlayEvent>(170);
        for (var n = 1; n <= 170; n++)
        {
            int Someone() => lines[rng.Next(lines.Count)].PlayerId.Value;
            events.Add(new PlayEvent(
                n, ((n - 1) / 43) + 1, rng.Next(0, 900), rng.Next(1, 5), rng.Next(1, 16), rng.Next(1, 100),
                Calls[rng.Next(Calls.Length)], Outcomes[rng.Next(Outcomes.Length)], rng.Next(-8, 40),
                rng.Next(2) == 0 ? Someone() : null, rng.Next(2) == 0 ? Someone() : null, rng.Next(2) == 0 ? Someone() : null,
                Someone(), Matchups[rng.Next(Matchups.Length)], Someone(), Someone(), (float)Math.Round(rng.NextDouble(), 2)));
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(events);
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
        {
            brotli.Write(json);
        }

        return (output.ToArray(), json.Length);
    }
}
