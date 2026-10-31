using GridironFrontOffice.Core.Leagues;

namespace GridironFrontOffice.Core.Tests.Leagues;

/// <summary>CALENDAR_AND_PHASES.md §2: camp opens on the fourth Tuesday of July, after the July 1 that ages are drawn on.</summary>
public sealed class LeagueCalendarTests
{
	public static TheoryData<int> Seasons { get; } = new(Enumerable.Range(2026, 35));

	[Theory]
	[MemberData(nameof(Seasons))]
	public void Camp_opens_on_the_fourth_Tuesday_of_July(int season)
	{
		var opens = LeagueCalendar.TrainingCampOpens(season);

		opens.Year.ShouldBe(season);
		opens.Month.ShouldBe(7);
		opens.DayOfWeek.ShouldBe(DayOfWeek.Tuesday);
		opens.Day.ShouldBeInRange(22, 28, "The fourth Tuesday: three Tuesdays of July come before it.");
	}

	[Theory]
	[InlineData(2025, 22)]   // July 1 is a Tuesday: it's the first, so the fourth is the earliest possible
	[InlineData(2026, 28)]   // July 1 is a Wednesday: the first Tuesday is July 7, so the fourth is the latest possible
	[InlineData(2027, 27)]
	public void The_edges_land_on_the_right_day(int season, int day)
	{
		LeagueCalendar.TrainingCampOpens(season).ShouldBe(new DateOnly(season, 7, day));
	}
}
