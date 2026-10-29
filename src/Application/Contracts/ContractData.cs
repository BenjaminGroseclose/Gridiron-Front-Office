using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Contracts;

// The built-in contract data exactly as data/contracts/contracts.json holds it (F-12 §4.9, the M1 slice). Definitions,
// not read models, so no Dto suffix (D-070). Money is long dollars; enum dictionary keys are the C# names; a field added
// later gets a default value, so older files still load (D-077).

/// <summary>
/// Everything the contract generator (WP4) reads: the cap, the band and the premium line the creation trades keep to,
/// the minimums, the rookie scale and the veteran pay model.
/// </summary>
/// <param name="PremiumShare">A cap hit over this share of the cap is a premium deal: the creation trades never give a team a second one at one position (F-12 §4.8, D-086).</param>
public sealed record ContractData(
    long Cap,
    Utilization Utilization,
    double PremiumShare,
    IReadOnlyList<MinimumSalary> MinimumSalaries,
    RookieScale RookieScale,
    UndraftedDeal Undrafted,
    VeteranDeals Veterans);

/// <summary>Where a team's total must land at creation, as shares of the cap (F-12 §4.3).</summary>
public sealed record Utilization(double Min, double Max);

/// <summary>A step of the minimum-salary ladder: the minimum base salary from this many credited seasons on (F-12 §4.4).</summary>
public sealed record MinimumSalary(int CreditedSeasons, long Salary);

/// <summary>Four-year rookie deals by round, linear within a round (F-12 §4.5).</summary>
public sealed record RookieScale(int Years, IReadOnlyList<double> BaseWeights, IReadOnlyList<RookieRound> Rounds);

/// <summary>A round's first and last pick totals, and the shares of the total paid as a bonus and guaranteed.</summary>
public sealed record RookieRound(int Round, long FirstPick, long LastPick, double BonusShare, double GuaranteeShare);

/// <summary>The undrafted rookie's deal: minimum salaries over <see cref="Years"/> with a flat bonus (F-12 §4.6).</summary>
public sealed record UndraftedDeal(int Years, long Bonus);

/// <summary>
/// The veteran pay model (F-12 §4.7): APY from the player's league-wide rank at his position through one curve, with
/// a maximum per position (the minimum is his minimum salary) and a wiggle; lengths and shares by contract tier.
/// </summary>
public sealed record VeteranDeals(
    double Curve,
    Wiggle Wiggle,
    IReadOnlyDictionary<Position, long> MaxApy,
    IReadOnlyDictionary<PlayerTier, IReadOnlyList<LengthBand>> Lengths,
    IReadOnlyDictionary<PlayerTier, Share> BonusShare,
    IReadOnlyDictionary<PlayerTier, Share> GuaranteeShare,
    double BaseEscalation);

/// <summary>A multiplicative nudge on a drawn APY: N(0, <see cref="Sd"/>), clamped to ±<see cref="Max"/>.</summary>
public sealed record Wiggle(double Sd, double Max);

/// <summary>Contract years for a player of up to <see cref="ThroughAge"/>, drawn uniformly from Min to Max.</summary>
public sealed record LengthBand(int ThroughAge, int Min, int Max);

/// <summary>A share of the total value, drawn uniformly from Min to Max.</summary>
public sealed record Share(double Min, double Max);
