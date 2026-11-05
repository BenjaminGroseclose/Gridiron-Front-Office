using GridironFrontOffice.Core.Leagues;

namespace GridironFrontOffice.UI.Football;

/// <summary>
/// The crest in a 100 × 100 box (F-02 §4.4). Every number is an int, so no culture can put a comma into the SVG.
/// Drawn and checked in Claude's preview (guide §8); tune it by eye on /_gallery/teams.
/// </summary>
public static class CrestGeometry
{
	/// <summary>The field: the shape's outline, also its clip path and its rim.</summary>
	public static string Field(CrestShape shape) => shape switch
	{
		CrestShape.Shield => "M50 4 L90 13 V46 C90 70 73 86 50 96 C27 86 10 70 10 46 V13 Z",
		CrestShape.Roundel => "M50 4 A46 46 0 1 1 50 96 A46 46 0 1 1 50 4 Z",
		CrestShape.Hexagon => "M50 4 L90 27 V73 L50 96 L10 73 V27 Z",
		CrestShape.Diamond => "M50 3 L97 50 L50 97 L3 50 Z",
		CrestShape.Banner => "M12 4 H88 V94 L50 78 L12 94 Z",
		_ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
	};

	/// <summary>The stripe, drawn past the edges and clipped to the field; null for None.</summary>
	public static string? Stripe(CrestStripe stripe) => stripe switch
	{
		CrestStripe.None => null,
		CrestStripe.Bar => "M42 0 H58 V100 H42 Z",
		CrestStripe.TwinBars => "M28 0 H37 V100 H28 Z M63 0 H72 V100 H63 Z",
		CrestStripe.Chevron => "M0 60 L50 84 L100 60 V73 L50 97 L0 73 Z",
		CrestStripe.Sash => "M0 -12 L112 100 L100 112 L-12 0 Z",
		_ => throw new ArgumentOutOfRangeException(nameof(stripe), stripe, null),
	};

	/// <summary>The monogram's font size: 52, 42 or 31 for one, two or three letters, and about 82% of that in a diamond.</summary>
	public static int MonogramSize(CrestShape shape, int letters) => (shape, Math.Clamp(letters, 1, 3)) switch
	{
		(CrestShape.Diamond, 1) => 43,
		(CrestShape.Diamond, 2) => 34,
		(CrestShape.Diamond, _) => 25,
		(_, 1) => 52,
		(_, 2) => 42,
		_ => 31,
	};

	/// <summary>The monogram's vertical center: a shield's and a banner's sit above the middle of the box.</summary>
	public static int MonogramY(CrestShape shape) => shape switch
	{
		CrestShape.Shield => 47,
		CrestShape.Banner => 44,
		_ => 50,
	};
}