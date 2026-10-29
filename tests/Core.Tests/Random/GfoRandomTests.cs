using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Core.Tests.Random;

/// <summary>
/// D-012: xoshiro256** seeded through SplitMix64. The reference values come from an independent implementation
/// (M1_WP1_GUIDE.md §2.3); statistical checks use fixed seeds, so they never flake.
/// </summary>
public sealed class GfoRandomTests
{
    private const int Draws = 100_000;

    [Fact]
    public void SplitMix64_matches_its_published_first_value() =>
        new SplitMix64(0).Next().ShouldBe(0xE220A8397B1DCDAFUL);

    [Theory]
    [InlineData(0UL, new ulong[] { 11091344671253066420, 13793997310169335082, 1900383378846508768, 7684712102626143532, 13521403990117723737 })]
    [InlineData(42UL, new ulong[] { 1546998764402558742, 6990951692964543102, 12544586762248559009, 17057574109182124193, 18295552978065317476 })]
    public void The_first_draws_match_the_reference(ulong seed, ulong[] expected)
    {
        var random = new GfoRandom(seed);

        expected.Select(_ => random.NextUInt64()).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0UL, new[] { 0.6012629994179048, 0.7477740925472398, 0.10301998939503632 })]
    [InlineData(42UL, new[] { 0.08386297105988216, 0.3789802506626686, 0.6800434110281394 })]
    public void The_first_doubles_match_the_reference(ulong seed, double[] expected)
    {
        var random = new GfoRandom(seed);

        expected.Select(_ => random.NextDouble()).ShouldBe(expected);
    }

    [Fact]
    public void The_same_seed_gives_the_same_draws_and_another_seed_does_not()
    {
        Draw(7).ShouldBe(Draw(7));
        Draw(7).ShouldNotBe(Draw(8));

        static ulong[] Draw(ulong seed)
        {
            var random = new GfoRandom(seed);
            return [.. Enumerable.Range(0, 1_000).Select(_ => random.NextUInt64())];
        }
    }

    [Fact]
    public void NextDouble_stays_in_zero_to_one_and_averages_a_half()
    {
        var random = new GfoRandom(1);
        var values = Enumerable.Range(0, Draws).Select(_ => random.NextDouble()).ToList();

        values.ShouldAllBe(v => v >= 0.0 && v < 1.0);
        values.Average().ShouldBe(0.5, tolerance: 0.005);
    }

    [Fact]
    public void Next_stays_in_bounds_and_spreads_evenly()
    {
        var random = new GfoRandom(2);
        var buckets = new int[10];
        for (var i = 0; i < Draws; i++)
        {
            buckets[random.Next(0, 10)]++;
        }

        buckets.ShouldAllBe(count => Math.Abs(count - (Draws / 10)) <= Draws / 10 * 0.05);
        Enumerable.Range(0, 1_000).Select(_ => random.Next(-3, 4)).ShouldAllBe(v => v >= -3 && v < 4);
        random.Next(int.MinValue, int.MaxValue).ShouldBeInRange(int.MinValue, int.MaxValue - 1);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4)]
    public void Next_needs_a_non_empty_range(int min, int max) =>
        Should.Throw<ArgumentOutOfRangeException>(() => new GfoRandom(3).Next(min, max));

    [Fact]
    public void Chance_never_happens_at_zero_and_always_at_one()
    {
        var random = new GfoRandom(4);

        Enumerable.Range(0, 1_000).ShouldAllBe(_ => !random.Chance(0) && random.Chance(1));
    }

    [Fact]
    public void Normal_has_the_asked_for_mean_and_spread()
    {
        var standard = Sample(new GfoRandom(5), mean: 0, standardDeviation: 1);
        standard.Mean.ShouldBe(0, tolerance: 0.02);
        standard.StandardDeviation.ShouldBe(1, tolerance: 0.02);

        var scaled = Sample(new GfoRandom(6), mean: 74, standardDeviation: 7);   // e.g. a starter's OVR (F-03 §4.10)
        scaled.Mean.ShouldBe(74, tolerance: 0.15);
        scaled.StandardDeviation.ShouldBe(7, tolerance: 0.15);

        static (double Mean, double StandardDeviation) Sample(GfoRandom random, double mean, double standardDeviation)
        {
            var values = Enumerable.Range(0, Draws).Select(_ => random.Normal(mean, standardDeviation)).ToList();
            var average = values.Average();
            return (average, Math.Sqrt(values.Average(v => (v - average) * (v - average))));
        }
    }

    [Fact]
    public void Draw_stays_within_the_distributions_bounds()
    {
        var random = new GfoRandom(5);
        var values = Enumerable.Range(0, Draws).Select(_ => random.Draw(new Distribution(50, 30, Min: 40, Max: 60))).ToList();

        values.ShouldAllBe(v => v >= 40 && v <= 60);
        values.ShouldContain(40);
        values.ShouldContain(60);
    }

    [Fact]
    public void Draw_is_exactly_one_Normal_even_with_no_spread()
    {
        // WP4 guide §4.1: a data change to a spread must never shift the draws after it.
        var drawn = new GfoRandom(9);
        var plain = new GfoRandom(9);

        drawn.Draw(new Distribution(10, 2)).ShouldBe(plain.Normal(10, 2));
        drawn.Draw(new Distribution(5, 0)).ShouldBe(5);
        plain.Normal();
        drawn.NextUInt64().ShouldBe(plain.NextUInt64());
    }
}
