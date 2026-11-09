using GridironFrontOffice.Core.Leagues;

namespace GridironFrontOffice.UI.Football;

/// <summary>The phases as the screens name them (CALENDAR_AND_PHASES.md §2). The enum's names aren't display text.</summary>
public static class Phases
{
	public static string Text(this Phase phase) => phase switch
	{
		Phase.TrainingCamp => "Training Camp",
		Phase.RegularSeason => "Regular Season",
		Phase.Playoffs => "Playoffs",
		Phase.SeasonWrap => "Season Wrap",
		Phase.ResignWindow => "Re-sign & Tag Window",
		Phase.Combine => "Combine",
		Phase.FreeAgency => "Free Agency",
		Phase.PreDraft => "Pre-Draft",
		Phase.Draft => "Draft",
		Phase.OffseasonProgram => "Offseason Program",
		_ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
	};
}
