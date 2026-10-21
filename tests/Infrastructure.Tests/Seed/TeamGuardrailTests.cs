using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// F-02 §4.3 against the shipped team set: the 2 × 4 × 4 structure, no real team's name, stadium or colors, and every
/// color readable and distinct. Each test lists every offender, so one run names every team to fix. A failure means
/// the data changes (F-02 Appendix A records how), never a looser threshold without a written reason.
/// </summary>
public sealed class TeamGuardrailTests
{
    private const double DistinctDeltaE = 20;     // primaries in a division or a shared city, at least this far apart
    private const double EchoDeltaE = 12;         // both colors this close to a real NFL pair is an echo
    private const double MinOnColorContrast = 4.5;

    private static readonly TeamSet Set = new EmbeddedSeedData(NullLogger<EmbeddedSeedData>.Instance).TeamSet;

    [Fact]
    public void The_league_is_2_conferences_of_4_divisions_of_4_teams_and_every_team_is_in_exactly_one()
    {
        List<string> problems = [];
        if (Set.Conferences.Count != 2)
        {
            problems.Add($"{Set.Conferences.Count} conferences, not 2");
        }

        foreach (var conference in Set.Conferences.Where(c => c.Divisions.Count != 4))
        {
            problems.Add($"{conference.Name} has {conference.Divisions.Count} divisions, not 4");
        }

        foreach (var (conference, division) in Divisions().Where(d => d.Division.Teams.Count != 4))
        {
            problems.Add($"{conference.Name} {division.Name} has {division.Teams.Count} teams, not 4");
        }

        var placed = Divisions().SelectMany(d => d.Division.Teams).ToList();
        var abbrs = Set.Teams.Select(t => t.Abbr).ToList();
        problems.AddRange(placed.GroupBy(a => a).Where(g => g.Count() > 1).Select(g => $"{g.Key} is in {g.Count()} divisions"));
        problems.AddRange(abbrs.Except(placed).Select(a => $"{a} is in no division"));
        problems.AddRange(placed.Except(abbrs).Select(a => $"the division entry {a} isn't a team"));

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Abbreviations_are_2_or_3_capital_letters_and_unique()
    {
        var problems = Set.Teams
            .Where(t => t.Abbr.Length is < 2 or > 3 || !t.Abbr.All(char.IsAsciiLetterUpper))
            .Select(t => $"{t.Abbr} isn't 2–3 capital letters")
            .Concat(Set.Teams.GroupBy(t => t.Abbr).Where(g => g.Count() > 1).Select(g => $"{g.Key} is used {g.Count()} times"));

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Colors_are_RRGGBB_and_monograms_are_1_to_3_characters()
    {
        var badColors =
            from t in Set.Teams
            from color in new[] { t.Colors.Primary, t.Colors.Secondary, t.Colors.Accent }
            where color.Length != 7 || color[0] != '#' || !color[1..].All(char.IsAsciiHexDigit)
            select $"{t.Abbr}: {color} isn't #RRGGBB";
        var badMonograms = Set.Teams
            .Where(t => t.Crest.Monogram.Trim().Length is < 1 or > 3)
            .Select(t => $"{t.Abbr}: monogram \"{t.Crest.Monogram}\" isn't 1–3 characters");

        badColors.Concat(badMonograms).ShouldBeEmpty();
    }

    [Fact]
    public void Teams_sharing_a_metro_play_in_different_conferences()
    {
        var conferenceOf = Divisions()
            .SelectMany(d => d.Division.Teams.Select(abbr => (abbr, d.Conference.Key)))
            .ToLookup(x => x.abbr, x => x.Key);

        var problems = Set.Teams
            .GroupBy(t => t.Metro)
            .Where(g => g.Count() > 1 && g.Select(t => conferenceOf[t.Abbr].FirstOrDefault()).Distinct().Count() < g.Count())
            .Select(g => $"{g.Key}: {string.Join(" and ", g.Select(t => t.Abbr))} share a conference");

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Teams_sharing_a_city_label_differ_in_monogram_and_primary_color()
    {
        var problems =
            from city in Set.Teams.GroupBy(t => t.City)
            from pair in Pairs(city.ToList())
            let deltaE = ColorChecks.DeltaE2000(pair.A.Colors.Primary, pair.B.Colors.Primary)
            where pair.A.Crest.Monogram == pair.B.Crest.Monogram || deltaE < DistinctDeltaE
            select $"{pair.A.Abbr}/{pair.B.Abbr} ({city.Key}): monograms {pair.A.Crest.Monogram}/{pair.B.Crest.Monogram}, primaries ΔE {deltaE:F1}";

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void No_nickname_is_a_pro_football_name()
    {
        var football = RepoFiles.Names("football-teams.json");

        Set.Teams.Where(t => football.Contains(RepoFiles.NameKey(t.Nickname)))
            .Select(t => $"{t.Abbr} {t.Nickname}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void No_nickname_is_a_major_league_name_in_its_own_metro()
    {
        var majors = RepoFiles.Blocklist<Dictionary<string, string[]>>("major-league-teams.json");

        var problems = Set.Teams.Select(t =>
            !majors.TryGetValue(t.Metro, out var names) ? $"{t.Abbr}: major-league-teams.json has no {t.Metro}"
            : names.Any(n => RepoFiles.NameKey(n) == RepoFiles.NameKey(t.Nickname)) ? $"{t.Abbr} {t.Nickname} plays in {t.Metro}"
            : null);

        problems.OfType<string>().ShouldBeEmpty();
    }

    [Fact]
    public void No_stadium_is_a_real_stadium_name()
    {
        var stadiums = RepoFiles.Names("stadiums.json");

        Set.Teams.Where(t => stadiums.Contains(RepoFiles.NameKey(t.Stadium.Name)))
            .Select(t => $"{t.Abbr}: {t.Stadium.Name}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void No_team_echoes_the_colors_of_its_metros_NFL_team()
    {
        var nfl = RepoFiles.Blocklist<Dictionary<string, MetroNflTeam[]>>("metro-nfl-colors.json");

        var problems = Set.Teams.SelectMany(t =>
            !nfl.TryGetValue(t.Metro, out var realTeams) ? [$"{t.Abbr}: metro-nfl-colors.json has no {t.Metro}"]
            : realTeams.Where(real => Echoes(t.Colors, real.Colors[0], real.Colors[1]))
                .Select(real => $"{t.Abbr} {t.Colors.Primary}/{t.Colors.Secondary} echoes the {real.Team}"));

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Primaries_in_a_division_are_clearly_different()
    {
        var problems =
            from d in Divisions()
            from pair in Pairs(TeamsOf(d.Division))
            let deltaE = ColorChecks.DeltaE2000(pair.A.Colors.Primary, pair.B.Colors.Primary)
            where deltaE < DistinctDeltaE
            select $"{pair.A.Abbr} {pair.A.Colors.Primary} / {pair.B.Abbr} {pair.B.Colors.Primary}: ΔE {deltaE:F1}";

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Text_on_every_primary_is_readable()
    {
        var problems =
            from t in Set.Teams
            let contrast = ColorChecks.OnColorContrast(t.Colors.Primary)
            where contrast < MinOnColorContrast
            select $"{t.Abbr} {t.Colors.Primary}: {contrast:F2}:1";

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void Crests_in_a_division_differ_in_shape_or_stripe()
    {
        var problems =
            from d in Divisions()
            from same in TeamsOf(d.Division).GroupBy(t => (t.Crest.Shape, t.Crest.Stripe))
            where same.Count() > 1
            select $"{string.Join(" and ", same.Select(t => t.Abbr))} share {same.Key.Shape} · {same.Key.Stripe}";

        problems.ShouldBeEmpty();
    }

    /// <summary>
    /// An echo is both colors within <see cref="EchoDeltaE"/> of the real pair, in either order: one shared color
    /// alone is fine (F-02 §4.3).
    /// </summary>
    private static bool Echoes(TeamColors colors, string realA, string realB)
    {
        bool Close(string ours, string real) => ColorChecks.DeltaE2000(ours, real) < EchoDeltaE;

        return (Close(colors.Primary, realA) && Close(colors.Secondary, realB))
            || (Close(colors.Primary, realB) && Close(colors.Secondary, realA));
    }

    private static IEnumerable<(ConferenceDefinition Conference, DivisionDefinition Division)> Divisions() =>
        Set.Conferences.SelectMany(c => c.Divisions.Select(d => (c, d)));

    // Tolerates a broken structure, so the structure test reports it instead of every test throwing.
    private static List<TeamDefinition> TeamsOf(DivisionDefinition division) =>
        [.. division.Teams.SelectMany(abbr => Set.Teams.Where(t => t.Abbr == abbr).Take(1))];

    private static IEnumerable<(TeamDefinition A, TeamDefinition B)> Pairs(List<TeamDefinition> teams) =>
        teams.SelectMany((a, i) => teams.Skip(i + 1).Select(b => (a, b)));
}
