using GridironFrontOffice.Core.Leagues;

namespace GridironFrontOffice.Application.Leagues;

// The built-in league data exactly as data/league/*.json holds it (D-035). These are definitions, not read
// models, so no Dto suffix (D-070). A field added later gets a default value, so older files still load (D-077).

/// <summary>The league's 32 teams in 2 conferences of 4 divisions (F-02 §5).</summary>
public sealed record TeamSet(string Name, IReadOnlyList<ConferenceDefinition> Conferences, IReadOnlyList<TeamDefinition> Teams);

public sealed record ConferenceDefinition(string Key, string Name, IReadOnlyList<DivisionDefinition> Divisions);

/// <summary>A division and its teams, by abbreviation.</summary>
public sealed record DivisionDefinition(string Key, string Name, IReadOnlyList<string> Teams);

/// <summary>One team. <see cref="Metro"/> is shared by a two-team metro's teams and used only by the data tests.</summary>
public sealed record TeamDefinition(string Abbr, string City, string Nickname, string Metro,
    TeamColors Colors, CrestDefinition Crest, StadiumDefinition Stadium);

/// <summary>Colors as <c>#RRGGBB</c>.</summary>
public sealed record TeamColors(string Primary, string Secondary, string Accent);

public sealed record CrestDefinition(CrestShape Shape, string Monogram, CrestStripe Stripe);

public sealed record StadiumDefinition(string Name, int Capacity, Roof Roof, Surface Surface, ClimateZone Climate);

/// <summary>The colleges prospects come from (D-035).</summary>
public sealed record CollegeList(string Name, IReadOnlyList<CollegeDefinition> Colleges);

public sealed record CollegeDefinition(string Name, CollegeRegion Region, CollegeTier Tier);
