using System.Globalization;

namespace GridironFrontOffice.UI.Components.Formatting;

public static class Money
{
	private const string Minus = "\u2212";

	/// <summary>
	/// Dollars as screens show them (F-12 §8): "$12.4M" from $999,500, "$850K" from $1,000, "$0" below, "−$3.2M" when
	/// negative. Rounded half up on the magnitude, in integers, the same in every culture.
	/// </summary>
	public static string Format(long dollars)
	{
		var sign = dollars < 0 ? Minus : string.Empty;
		var size = Math.Abs(dollars);

		// If greater than a million
		if (size >= 999_500)
		{
			var tenths = (size + 50_000) / 100_000;
			return string.Create(CultureInfo.InvariantCulture, $"{sign}${tenths / 10:#,0}.{tenths % 10}M");
		}

		return size >= 1_000
			? string.Create(CultureInfo.InvariantCulture, $"{sign}${(size + 500) / 1_000}K")
			: string.Create(CultureInfo.InvariantCulture, $"{sign}${size}");
	}
}