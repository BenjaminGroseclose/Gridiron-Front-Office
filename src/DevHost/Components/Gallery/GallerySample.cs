using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.DevHost.Components.Gallery;

/// <summary>
/// Made-up rows for the gallery's grid: 2,000 fictional players with 25 columns, the size Spike S0b tested. Generated
/// from a fixed seed with a tiny LCG, so every run shows the same rows (the gallery is a visual check, not a sim).
/// </summary>
internal static class GallerySample
{
    public static IReadOnlyList<string> AttributeNames { get; } =
        ["SPD", "STR", "AGI", "ACC", "AWR", "CTH", "CAR", "THP", "THA", "BLK", "TKL", "PRS", "COV", "RTE", "JMP", "STA", "INJ", "TGH"];

    private static readonly string[] First = ["Marcus", "Dante", "Theo", "Jalen", "Owen", "Isaiah", "Caleb", "Rafael", "Kenji", "Malik", "Ezra", "Tobias", "Andre", "Silas", "Nico", "Desmond"];
    private static readonly string[] Last = ["Okafor", "Mensah", "Lindqvist", "Hale", "Moreau", "Castellano", "Brightwater", "Oduya", "Fairweather", "Koenig", "Abernathy", "Delacroix", "Whitlock", "Ashby", "Ferreira", "Nakamura"];
    private static readonly Position[] Positions =
        [Position.QB, Position.RB, Position.WR, Position.TE, Position.OT, Position.OG, Position.EDGE, Position.DT, Position.LB, Position.CB, Position.S, Position.K];
    private static readonly PotentialGrade[] Grades = Enum.GetValues<PotentialGrade>();
    private static readonly string[] Teams = ["PHI", "WAS", "BAL", "CHI", "GB", "MIN", "KC", "ATL", "CHA", "JAX", "LAC", "LV"];

    public static IReadOnlyList<GalleryPlayer> Players { get; } = Generate(2000);

    private static List<GalleryPlayer> Generate(int count)
    {
        var state = 20261018u;
        int Next(int max)
        {
            state = unchecked((state * 1664525u) + 1013904223u);
            return (int)((state >> 8) % (uint)max);
        }

        var players = new List<GalleryPlayer>(count);
        for (var id = 1; id <= count; id++)
        {
            var overall = 48 + Next(50);
            var spread = 1 + Next(6);
            var grade = Grades[Next(Grades.Length)];
            players.Add(new GalleryPlayer(
                ID: id,
                Name: $"{First[Next(First.Length)]} {Last[Next(Last.Length)]}",
                Position: Positions[Next(Positions.Length)],
                Team: Teams[Next(Teams.Length)],
                Age: 21 + Next(14),
                Overall: new PerceivedRating(overall, 0),
                OverallLow: Math.Max(0, overall - spread),
                OverallHigh: Math.Min(99, overall + spread),
                Potential: new PotentialRange(grade, grade, grade),
                CapHit: 750_000L + (Next(400) * 100_000L),
                Attributes: [.. AttributeNames.Select(_ => 40 + Next(60))]));
        }

        return players;
    }
}

internal sealed record GalleryPlayer(
    int ID,
    string Name,
    Position Position,
    string Team,
    int Age,
    PerceivedRating Overall,
    int OverallLow,
    int OverallHigh,
    PotentialRange Potential,
    long CapHit,
    IReadOnlyList<int> Attributes);
