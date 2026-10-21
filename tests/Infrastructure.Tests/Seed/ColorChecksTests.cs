namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>The color math matches published and independently computed values before the guardrails rely on it.</summary>
public sealed class ColorChecksTests
{
    [Theory]
    [InlineData(50, 2.6772, -79.7751, 50, 0, -82.7485, 2.0425)]   // Sharma, Wu and Dalal (2005)
    [InlineData(50, 0, 0, 50, -1, 2, 2.3669)]                      // a gray: the zero-chroma case
    [InlineData(50, 2.5, 0, 73, 25, -18, 27.1492)]
    [InlineData(50, 2.5, 0, 58, 24, 15, 19.4535)]
    public void CIEDE2000_matches_the_published_pairs(double l1, double a1, double b1, double l2, double a2, double b2, double expected) =>
        ColorChecks.DeltaE2000((l1, a1, b1), (l2, a2, b2)).ShouldBe(expected, tolerance: 0.0001);

    [Theory]
    [InlineData("#A6192E", "#1F3F7A", 38.0003)]
    [InlineData("#FFFFFF", "#000000", 100.0000)]
    public void CIEDE2000_from_hex_goes_through_Lab_correctly(string a, string b, double expected) =>
        ColorChecks.DeltaE2000(a, b).ShouldBe(expected, tolerance: 0.0001);

    [Theory]
    [InlineData("#A6192E", "#FFFFFF", 7.5003)]
    [InlineData("#B88A1B", "#0B0D12", 6.1907)]
    public void Contrast_matches_WCAG(string a, string b, double expected) =>
        ColorChecks.Contrast(a, b).ShouldBe(expected, tolerance: 0.0001);
}
