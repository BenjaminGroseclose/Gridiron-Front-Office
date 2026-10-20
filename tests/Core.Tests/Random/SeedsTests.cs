using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Core.Tests.Random;

/// <summary>D-012's derived seeds: FNV-1a 64, then SplitMix64's Mix (reference values: M1_WP1_GUIDE.md §2.4).</summary>
public sealed class SeedsTests
{
    [Theory]
    [InlineData(42UL, "gen", new long[] { 2026, 0 }, 5954498215675448378UL)]
    [InlineData(42UL, "gen", new long[] { 2026, 1 }, 1676545436668552088UL)]
    [InlineData(43UL, "gen", new long[] { 2026, 0 }, 16717909402313610759UL)]
    [InlineData(42UL, "perception", new long[] { 7, 1234, 2026 }, 18163511695943298027UL)]
    public void Derive_matches_the_reference_values(ulong seed, string purpose, long[] parts, ulong expected) =>
        Seeds.Derive(seed, purpose, parts).ShouldBe(expected);

    [Fact]
    public void Every_input_changes_the_seed()
    {
        var baseline = Seeds.Derive(42, SeedPurpose.Generation, 2026, 0);

        Seeds.Derive(43, SeedPurpose.Generation, 2026, 0).ShouldNotBe(baseline);
        Seeds.Derive(42, SeedPurpose.Perception, 2026, 0).ShouldNotBe(baseline);
        Seeds.Derive(42, SeedPurpose.Generation, 2027, 0).ShouldNotBe(baseline);
        Seeds.Derive(42, SeedPurpose.Generation, 2026, 1).ShouldNotBe(baseline);
    }

    [Fact]
    public void Part_order_and_part_count_matter()
    {
        Seeds.Derive(42, "p", 1, 2).ShouldNotBe(Seeds.Derive(42, "p", 2, 1));
        Seeds.Derive(42, "gen").ShouldNotBe(Seeds.Derive(42, "gen", 0));
    }

    [Fact]
    public void A_purpose_is_required() =>
        Should.Throw<ArgumentException>(() => Seeds.Derive(42, ""));
}
