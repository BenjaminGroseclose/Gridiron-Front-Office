using System.Globalization;

namespace GridironFrontOffice.DevHost.Components.Spike;

internal sealed record SpikePlayer(
    int Id, string Name, string Team, string Position, int Age,
    int Overall, int OverallLow, int OverallHigh, string Potential, long CapHit, int[] Attributes);

/// <summary>Spike S0b: 2,000 synthetic players, the size of a full league's rosters plus free agents.</summary>
internal static class SpikeData
{
    public static readonly IReadOnlyList<string> AttributeNames =
        ["SPD", "ACC", "AGI", "STR", "STA", "TGH", "AWR", "DIS", "THP", "SAC", "MAC", "DAC", "CTH", "RTE", "BTK", "ELU", "TAK", "PRS"];

    private static readonly string[] Teams = ["NYX", "PHI", "WAS", "BAL", "CHI", "GB", "MIN", "KC", "ATL", "CHA", "NSH", "JAX", "LAC", "SF", "SEA", "LV",
        "NYK", "BOS", "BUF", "PIT", "DET", "CLE", "CIN", "IND", "MIA", "TPA", "NO", "HOU", "LAN", "PHX", "DEN", "DAL"];

    private static readonly string[] Positions = ["QB", "RB", "FB", "WR", "TE", "OT", "OG", "C", "EDGE", "DT", "LB", "CB", "S", "K", "P", "LS"];
    private static readonly string[] Grades = ["A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F"];
    private static readonly string[] First = ["Marcus", "Tyrell", "Jordan", "Caleb", "Andre", "Luis", "Devon", "Isaiah", "Malik", "Owen", "Rashad", "Elijah", "Trent", "Dante", "Kofi"];
    private static readonly string[] Last = ["Okafor", "Mensah", "Reyes", "Lindqvist", "Haskins", "Toure", "Whitfield", "Brennan", "Castillo", "Ashworth", "Delacroix", "Moreau", "Pruitt", "Vance", "Sorensen"];

    public static IReadOnlyList<SpikePlayer> Players { get; } = Generate(2000);

    public static string Tier(int rating) => rating switch
    {
        >= 90 => "elite",
        >= 80 => "good",
        >= 70 => "avg",
        >= 60 => "below",
        _ => "poor",
    };

    public static string Money(long dollars) => dollars >= 1_000_000
        ? "$" + (dollars / 1_000_000d).ToString("0.0", CultureInfo.InvariantCulture) + "M"
        : "$" + (dollars / 1_000d).ToString("0", CultureInfo.InvariantCulture) + "K";

    private static List<SpikePlayer> Generate(int count)
    {
        var rng = new Random(7);
        var players = new List<SpikePlayer>(count);
        for (var i = 0; i < count; i++)
        {
            var overall = rng.Next(48, 97);
            var sigma = rng.Next(0, 7);
            players.Add(new SpikePlayer(
                i + 1,
                $"{First[rng.Next(First.Length)]} {Last[rng.Next(Last.Length)]}",
                Teams[i % Teams.Length],
                Positions[rng.Next(Positions.Length)],
                rng.Next(21, 37),
                overall,
                Math.Max(0, overall - sigma),
                Math.Min(99, overall + sigma),
                Grades[rng.Next(Grades.Length)],
                rng.Next(750, 45_000) * 1_000L,
                [.. Enumerable.Range(0, AttributeNames.Count).Select(_ => rng.Next(35, 99))]));
        }

        return players;
    }
}
