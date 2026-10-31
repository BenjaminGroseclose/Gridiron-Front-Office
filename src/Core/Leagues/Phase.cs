namespace GridironFrontOffice.Core.Leagues;

// Core/Leagues/Phase.cs
/// <summary>
/// The league year's phases in calendar order (CALENDAR_AND_PHASES.md §2). Saves store the name. M1–M4 leagues open
/// at TrainingCamp (D-049); the phases move from M3 on.
/// </summary>
public enum Phase
{
	TrainingCamp, RegularSeason, Playoffs, SeasonWrap, ResignWindow, Combine, FreeAgency, PreDraft, Draft, OffseasonProgram,
}

