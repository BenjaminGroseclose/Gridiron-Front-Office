namespace GridironFrontOffice.Core.Contracts;

/// <summary>
/// The salary cap's arithmetic (D-074, F-12): pure functions on long dollars. A signing bonus is paid up front but
/// counts against the cap spread evenly over the first five seasons of the contract at most (proration).
/// </summary>
public static class CapMath
{
    /// <summary>The longest contract a player can sign.</summary>
    public const int MaxContractYears = 7;

    /// <summary>A signing bonus is never spread over more seasons than this, however long the contract (the league's rule).</summary>
    public const int MaxProratedYears = 5;

    /// <summary>
    /// The part of the signing bonus that counts against the cap in year <paramref name="yearIndex"/> (0 is the first
    /// season) of a contract of <paramref name="years"/> seasons. Any remainder from the division goes in the first
    /// year, so the shares always add up to exactly the bonus.
    /// </summary>
    public static long ProratedBonus(long signingBonus, int years, int yearIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(signingBonus);
        ArgumentOutOfRangeException.ThrowIfLessThan(years, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(years, MaxContractYears);
        ArgumentOutOfRangeException.ThrowIfNegative(yearIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(yearIndex, years);

        var prorationYears = Math.Min(years, MaxProratedYears);
        if (yearIndex >= prorationYears)
        {
            return 0; // Year 6 or 7 of a long deal cannot carry a bonus
        }

        var share = signingBonus / prorationYears;
        var remainder = signingBonus % prorationYears;
        return yearIndex == 0 ? share + remainder : share;
    }

    /// <summary>A season's cap hit: its base salary plus that season's share of the signing bonus.</summary>
    public static long CapHit(long baseSalary, long signingBonus, int years, int yearIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseSalary);
        return baseSalary + ProratedBonus(signingBonus, years, yearIndex);
    }
}
