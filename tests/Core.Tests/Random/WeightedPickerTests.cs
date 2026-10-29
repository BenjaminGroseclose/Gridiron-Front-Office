using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Core.Tests.Random;

/// <summary>
/// M1 WP4 guide §4.1: picks in proportion to the weights, one NextDouble per pick, mapped to the first item whose
/// running total is above it. Every weight ≥ 0 and at least one > 0, checked when the picker is built.
/// </summary>
public sealed class WeightedPickerTests
{
	private const int Picks = 100_000;

	[Fact]
	public void Picks_follow_the_weights()
	{
		var picker = new WeightedPicker<string>([("a", 1), ("b", 2), ("c", 7)]);
		var random = new GfoRandom(11);

		var counts = Enumerable.Range(0, Picks).Select(_ => picker.Pick(random)).CountBy(item => item).ToDictionary();

		(counts["a"] / (double)Picks).ShouldBe(0.1, tolerance: 0.01);
		(counts["b"] / (double)Picks).ShouldBe(0.2, tolerance: 0.01);
		(counts["c"] / (double)Picks).ShouldBe(0.7, tolerance: 0.01);
	}

	[Fact]
	public void Each_pick_is_one_NextDouble_through_the_running_totals()
	{
		// Totals 2, 2, 7, 10: "b" has no room, so a draw below 2 is "a", below 7 is "c", the rest "d".
		var picker = new WeightedPicker<string>([("a", 2), ("b", 0), ("c", 5), ("d", 3)]);
		var random = new GfoRandom(3);
		var mirror = new GfoRandom(3);

		for (var i = 0; i < 10_000; i++)
		{
			var x = mirror.NextDouble() * 10;
			var expected = x < 2 ? "a" : x < 7 ? "c" : "d";

			picker.Pick(random).ShouldBe(expected);
		}
	}

	[Fact]
	public void Zero_weights_are_allowed_and_never_picked()
	{
		// Zeros at both ends, where a search over the running totals goes wrong first.
		var picker = new WeightedPicker<string>([("first", 0), ("a", 1), ("b", 1), ("last", 0)]);
		var random = new GfoRandom(5);

		Enumerable.Range(0, Picks).Select(_ => picker.Pick(random)).Distinct().Order().ShouldBe(["a", "b"]);
	}

	[Fact]
	public void A_single_item_is_always_picked()
	{
		var picker = new WeightedPicker<string>([("only", 3)]);
		var random = new GfoRandom(7);

		Enumerable.Range(0, 1_000).Select(_ => picker.Pick(random)).ShouldAllBe(item => item == "only");
	}

	[Fact]
	public void A_negative_weight_throws_when_built() =>
		Should.Throw<ArgumentException>(() => new WeightedPicker<string>([("a", 1), ("b", -0.5)]));

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void A_list_without_a_positive_weight_throws_when_built(int zeroWeights) =>
		Should.Throw<ArgumentException>(() =>
			new WeightedPicker<int>(Enumerable.Range(0, zeroWeights).Select(i => (i, 0.0))));
}
