using GridironFrontOffice.Application.Players;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>
/// The built-in data, loaded once (D-035): the league's teams and colleges (WP2) and everything the player generator
/// needs (WP3). Generators read only this; nothing hard-codes a list.
/// </summary>
public interface ISeedData
{
    TeamSet TeamSet { get; }
    CollegeList Colleges { get; }

    /// <summary>One profile per position: roster targets, ages, measurables, OVR weights and archetypes.</summary>
    PositionData Positions { get; }

    /// <summary>The league-wide generation settings: tiers, skills, athletic maps, hidden ratings, personality, targets.</summary>
    GenerationSettings Generation { get; }

    /// <summary>The jersey numbering rule by position.</summary>
    JerseyRules JerseyRules { get; }

    NameList FirstNames { get; }

    NameList LastNames { get; }

    HometownList Hometowns { get; }

    /// <summary>Names the generator re-rolls (F-03 §4.11): the one blocklist that ships.</summary>
    IReadOnlyList<string> FamousPeople { get; }
}
