using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.UI.Football;

namespace GridironFrontOffice.UI.Tests.Football;

/// <summary>
/// M1 WP7 slice 2 (guide §4): the crest's drawing in a 100 × 100 box. These pin its structure, not its path data, so
/// the geometry can be tuned by eye on /_gallery/teams without a test edit.
/// </summary>
public sealed class CrestGeometryTests
{
	[Fact]
	public void Every_shape_has_a_closed_field_of_its_own()
	{
		var fields = Enum.GetValues<CrestShape>().Select(CrestGeometry.Field).ToList();

		fields.Count.ShouldBe(5);
		fields.Distinct(StringComparer.Ordinal).Count().ShouldBe(5);
		fields.ShouldAllBe(d => d.StartsWith('M') && d.EndsWith('Z'));
	}

	[Fact]
	public void None_has_no_stripe_and_the_other_four_differ()
	{
		CrestGeometry.Stripe(CrestStripe.None).ShouldBeNull();

		var stripes = Enum.GetValues<CrestStripe>().Where(s => s != CrestStripe.None).Select(CrestGeometry.Stripe).ToList();
		stripes.Count.ShouldBe(4);
		stripes.ShouldAllBe(d => d != null && d.StartsWith('M') && d.EndsWith('Z'));
		stripes.Distinct(StringComparer.Ordinal).Count().ShouldBe(4);
	}

	[Fact]
	public void A_monogram_shrinks_with_each_letter_and_three_is_the_smallest()
	{
		foreach (var shape in Enum.GetValues<CrestShape>())
		{
			var (one, two, three) = (Size(shape, 1), Size(shape, 2), Size(shape, 3));

			one.ShouldBeGreaterThan(two, $"{shape}");
			two.ShouldBeGreaterThan(three, $"{shape}");
			Size(shape, 0).ShouldBe(one, $"{shape}: an empty monogram is sized as one letter");
			Size(shape, 4).ShouldBe(three, $"{shape}: four letters (an import's, M11) take the three-letter size");
			three.ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	public void A_diamonds_monogram_is_smaller_than_any_other_shapes()
	{
		// A diamond's middle is narrower than the other four's (F-02 §4.4: readable in every shape)
		for (var letters = 1; letters <= 3; letters++)
		{
			var diamond = Size(CrestShape.Diamond, letters);
			foreach (var shape in Enum.GetValues<CrestShape>().Where(s => s != CrestShape.Diamond))
			{
				Size(shape, letters).ShouldBeGreaterThan(diamond, $"{shape}, {letters} letters");
			}
		}
	}

	[Fact]
	public void A_shields_and_a_banners_monogram_sit_above_the_middle()
	{
		CrestGeometry.MonogramY(CrestShape.Shield).ShouldBeLessThan(50);
		CrestGeometry.MonogramY(CrestShape.Banner).ShouldBeLessThan(50);
		CrestGeometry.MonogramY(CrestShape.Roundel).ShouldBe(50);
		CrestGeometry.MonogramY(CrestShape.Hexagon).ShouldBe(50);
		CrestGeometry.MonogramY(CrestShape.Diamond).ShouldBe(50);
	}

	[Fact]
	public void An_unknown_shape_or_stripe_throws()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => CrestGeometry.Field((CrestShape)99));
		Should.Throw<ArgumentOutOfRangeException>(() => CrestGeometry.Stripe((CrestStripe)99));
	}

	private static int Size(CrestShape shape, int letters) => CrestGeometry.MonogramSize(shape, letters);
}
