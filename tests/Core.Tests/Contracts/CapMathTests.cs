using GridironFrontOffice.Core.Contracts;

namespace GridironFrontOffice.Core.Tests.Contracts;

/// <summary>D-074: the cap hit is the base salary plus the signing bonus prorated over at most five seasons.</summary>
public sealed class CapMathTests
{
    [Theory]
    [InlineData(9_000_000L, 3, new long[] { 3_000_000, 3_000_000, 3_000_000 })]
    [InlineData(10_000_000L, 7, new long[] { 2_000_000, 2_000_000, 2_000_000, 2_000_000, 2_000_000, 0, 0 })]
    [InlineData(10_000_001L, 3, new long[] { 3_333_335, 3_333_333, 3_333_333 })]
    public void The_bonus_spreads_over_at_most_five_seasons(long bonus, int years, long[] expected) =>
        Enumerable.Range(0, years).Select(i => CapMath.ProratedBonus(bonus, years, i)).ShouldBe(expected);

    [Fact]
    public void The_shares_always_add_up_to_exactly_the_bonus()
    {
        foreach (var years in Enumerable.Range(1, CapMath.MaxContractYears))
        {
            foreach (var bonus in new long[] { 0, 1, 4, 7, 999_999, 10_000_001, 123_456_789 })
            {
                Enumerable.Range(0, years).Sum(i => CapMath.ProratedBonus(bonus, years, i)).ShouldBe(bonus);
            }
        }
    }

    [Fact]
    public void A_cap_hit_is_base_plus_that_seasons_share() =>
        CapMath.CapHit(2_000_000, 9_000_000, 3, 1).ShouldBe(5_000_000);

    [Theory]
    [InlineData(1, 0, 0)]     // no years
    [InlineData(1, 8, 0)]     // longer than the 7-year maximum
    [InlineData(1, 3, 3)]     // a season past the contract
    [InlineData(1, 3, -1)]    // a season before it
    [InlineData(-1, 3, 0)]    // a negative bonus
    public void Impossible_contracts_throw(long bonus, int years, int yearIndex) =>
        Should.Throw<ArgumentOutOfRangeException>(() => CapMath.ProratedBonus(bonus, years, yearIndex));

    [Fact]
    public void A_negative_base_salary_throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => CapMath.CapHit(-1, 0, 3, 0));
}
