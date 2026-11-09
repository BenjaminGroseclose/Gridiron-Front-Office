using GridironFrontOffice.UI.Menu;

namespace GridironFrontOffice.UI.Tests.Menu;

/// <summary>M1 WP8 slice 1 (guide §4): New League's seed field (F-01 §4, D-097).</summary>
public sealed class SeedTextTests
{
    [Theory]
    [InlineData("42", 42UL)]
    [InlineData(" 42 ", 42UL)]
    [InlineData("007", 7UL)]
    [InlineData("0", 0UL)]
    [InlineData("18446744073709551615", ulong.MaxValue)]
    public void Digits_are_the_seed(string text, ulong seed) => SeedText.Read(text).ShouldBe(seed);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("4.2")]
    [InlineData("1e9")]
    [InlineData("12a")]
    [InlineData("1 000")]
    [InlineData("１２")]   // full-width digits: digits to char.IsDigit, but not ASCII
    [InlineData("18446744073709551616")]
    public void Anything_else_is_refused(string? text) => SeedText.Read(text).ShouldBeNull();

    [Fact]
    public void The_refusal_names_the_largest_seed() =>
        SeedText.Refused.ShouldBe($"A seed is a whole number, up to {ulong.MaxValue}.");

    [Fact]
    public void A_random_seed_is_one_to_nine_digits_and_reads_back_as_itself()
    {
        var seeds = Enumerable.Range(0, 2_000).Select(_ => SeedText.Random()).ToList();

        seeds.ShouldAllBe(s => s >= 1 && s <= SeedText.RandomMax);
        SeedText.RandomMax.ShouldBe(999_999_999UL);
        seeds.Distinct().Count().ShouldBeGreaterThan(1_990, "A new seed each time, near enough.");
        seeds.ShouldAllBe(s => SeedText.Read(s.ToString(System.Globalization.CultureInfo.InvariantCulture)) == s);
    }
}
