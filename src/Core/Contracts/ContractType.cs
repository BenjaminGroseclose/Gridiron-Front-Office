namespace GridironFrontOffice.Core.Contracts;

/// <summary>
/// How a contract came to be (F-12 §4.5–4.7). Saves store the name: rename only with a migration. M4 adds the tag and
/// extensions.
/// </summary>
public enum ContractType
{
    /// <summary>A four-year slotted deal for a drafted player (F-12 §4.5).</summary>
    Rookie,

    /// <summary>A negotiated deal shaped by the player's league-wide rank at his position (F-12 §4.7).</summary>
    Veteran,

    /// <summary>Minimum salaries: an undrafted rookie's three years, or a veteran's one (F-12 §4.6).</summary>
    Minimum,
}
