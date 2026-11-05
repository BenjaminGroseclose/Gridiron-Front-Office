using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.UI.Football;

public enum RatingTier { Poor, Below, Average, Good, Elite }
public static class RatingTiers
{
	/// <summary>The tier of a shown 0–99 rating. Tier the number the screen shows, never the raw estimate.</summary>
	public static RatingTier Of(int shown) => shown switch
	{
		>= 90 => RatingTier.Elite,
		>= 80 => RatingTier.Good,
		>= 70 => RatingTier.Average,
		>= 60 => RatingTier.Below,
		_ => RatingTier.Poor,
	};

	/// <summary>A potential grade on the rating scale, by its letter (§9 Q3): A elite, B good, C average, D below, F poor.</summary>
	public static RatingTier OfGrade(PotentialGrade grade) => grade switch
	{
		>= PotentialGrade.AMinus => RatingTier.Elite,
		>= PotentialGrade.BMinus => RatingTier.Good,
		>= PotentialGrade.CMinus => RatingTier.Average,
		PotentialGrade.D => RatingTier.Below,
		_ => RatingTier.Poor,
	};

	/// <summary>The class suffix in gfo-tokens.css: gfo-tier-elite … gfo-tier-poor.</summary>
	public static string Token(this RatingTier tier) => tier switch
	{
		RatingTier.Elite => "elite",
		RatingTier.Good => "good",
		RatingTier.Average => "avg",
		RatingTier.Below => "below",
		_ => "poor",
	};
}


public static class PotentialGrades
{
	/// <summary>The display map PotentialGrade's summary promises: "A+", "A", "A−" … "D", "F", with a real minus (U+2212).</summary>
	public static string Text(this PotentialGrade grade) => grade switch
	{
		PotentialGrade.APlus => "A+",
		PotentialGrade.A => "A",
		PotentialGrade.AMinus => "A−",
		PotentialGrade.BPlus => "B+",
		PotentialGrade.B => "B",
		PotentialGrade.BMinus => "B−",
		PotentialGrade.CPlus => "C+",
		PotentialGrade.C => "C",
		PotentialGrade.CMinus => "C−",
		PotentialGrade.D => "D",
		_ => "F",
	};
}