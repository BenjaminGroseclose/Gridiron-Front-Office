using System.Globalization;
using GridironFrontOffice.UI.Components.Formatting;

namespace GridironFrontOffice.UI.Components.Tests.Formatting;

/// <summary>
/// M1 WP7 slice 1 (guide §4): <see cref="Money.Format"/>, F-12 §8's "$12.4M". Millions with one decimal from $999,500,
/// whole thousands from $1,000, whole dollars below, rounded half up on the magnitude, a real minus, any culture.
/// </summary>
public sealed class MoneyTests
{
	private const string Minus = "−";

	[Theory]
	[InlineData(0L, "$0")]
	[InlineData(1L, "$1")]
	[InlineData(999L, "$999")]
	[InlineData(1_000L, "$1K")]
	[InlineData(1_499L, "$1K")]
	[InlineData(1_500L, "$2K")]
	[InlineData(850_000L, "$850K")]
	[InlineData(999_499L, "$999K")]
	[InlineData(999_500L, "$1.0M")]                // rounds to a thousand thousands, so it's shown in millions
	[InlineData(1_049_999L, "$1.0M")]
	[InlineData(1_050_000L, "$1.1M")]
	[InlineData(9_000_000L, "$9.0M")]              // F-12 §8: "$9.0M guaranteed remaining", the zero kept
	[InlineData(12_449_999L, "$12.4M")]
	[InlineData(12_450_000L, "$12.5M")]
	[InlineData(300_000_000L, "$300.0M")]          // the first-season cap
	[InlineData(999_949_999L, "$999.9M")]
	[InlineData(999_950_000L, "$1,000.0M")]        // thousands separators past $999.9M, and no B
	[InlineData(1_234_500_000L, "$1,234.5M")]
	public void An_amount_is_shown_compactly_and_rounded_half_up(long dollars, string shown) =>
		Money.Format(dollars).ShouldBe(shown);

	[Theory]
	[InlineData(-1L, "$1")]
	[InlineData(-850_000L, "$850K")]
	[InlineData(-3_200_000L, "$3.2M")]             // a team over the cap: negative cap space
	[InlineData(-12_450_000L, "$12.5M")]           // half up on the magnitude, so away from zero
	public void A_negative_amount_takes_a_real_minus_and_rounds_like_its_magnitude(long dollars, string magnitude)
	{
		var shown = Money.Format(dollars);

		shown.ShouldBe(Minus + magnitude);
		shown.ShouldNotContain("-");
	}

	[Fact]
	public void Every_culture_shows_the_same_text()
	{
		var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
		try
		{
			// German swaps the separators: a culture-sensitive format would write "$12,5M" and "$1.234,5M"
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

			Money.Format(12_450_000).ShouldBe("$12.5M");
			Money.Format(1_234_500_000).ShouldBe("$1,234.5M");
			Money.Format(850_000).ShouldBe("$850K");
			Money.Format(-3_200_000).ShouldBe(Minus + "$3.2M");
		}
		finally
		{
			(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
		}
	}
}
