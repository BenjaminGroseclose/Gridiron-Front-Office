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

// Core/Leagues/LeagueCalendar.cs
/// <summary>The calendar's dates (CALENDAR_AND_PHASES.md §2). M1 needs one; M3 adds the rest.</summary>
public static class LeagueCalendar
{
	/// <summary>
	/// The fourth Tuesday of July (July 22–28), when veterans report: after July 1, the date ages are drawn on.
	/// </summary>
	public static DateOnly TrainingCampOpens(int season)
	{
		var firstDayOfJuly = new DateTime(season, 7, 1);

		int daysToAdd = ((int)DayOfWeek.Tuesday - (int)firstDayOfJuly.DayOfWeek + 7) % 7;

		// 3. This is the date of the 1st occurrence in the month
		DateTime firstOccurrence = firstDayOfJuly.AddDays(daysToAdd);

		// 4. Add 7 days for each subsequent occurrence (x - 1)
		return DateOnly.FromDateTime(firstOccurrence.AddDays((4 - 1) * 7));
	}
}