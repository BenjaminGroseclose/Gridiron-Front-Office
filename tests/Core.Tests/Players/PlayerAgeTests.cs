using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Core.Tests.Players;

/// <summary>Player.AgeOn: whole years on the league date (WP6 §2), counted the way birthdays count them.</summary>
public sealed class PlayerAgeTests
{
	[Theory]
	[InlineData(7, 27, 28)]   // the day before the 29th birthday
	[InlineData(7, 28, 29)]   // the birthday: 10,592 days, which ÷ 365.25 still calls 28
	[InlineData(7, 29, 29)]   // the day after
	public void The_age_goes_up_on_the_birthday(int month, int day, int age)
	{
		Player.AgeOn(new DateOnly(1997, 7, 28), new DateOnly(2026, month, day)).ShouldBe(age);
	}

	[Theory]
	[InlineData(2026, 2, 27, 25)]
	[InlineData(2026, 2, 28, 26)]   // no February 29 this year: the birthday counts a day early
	[InlineData(2026, 3, 1, 26)]
	[InlineData(2028, 2, 28, 27)]   // a leap year: the birthday is the 29th itself
	[InlineData(2028, 2, 29, 28)]
	public void A_february_29_birthday_counts_on_february_28_in_other_years(int year, int month, int day, int age)
	{
		Player.AgeOn(new DateOnly(2000, 2, 29), new DateOnly(year, month, day)).ShouldBe(age);
	}

	[Theory]
	[InlineData(1, 25, 25)]    // born July 1: the drawn age all through camp
	[InlineData(2, 24, 25)]    // born July 2–28: a birthday between the draw and camp
	[InlineData(28, 24, 25)]
	[InlineData(29, 24, 24)]   // born after camp opens: still the drawn age
	public void Camp_shows_a_year_more_than_july_1_for_a_birthday_in_between(int julyDay, int onJuly1, int atCamp)
	{
		var born = new DateOnly(2001, 7, julyDay);

		Player.AgeOn(born, new DateOnly(2026, 7, 1)).ShouldBe(onJuly1);
		Player.AgeOn(born, LeagueCalendar.TrainingCampOpens(2026)).ShouldBe(atCamp);   // July 28
	}

	[Fact]
	public void Every_birth_date_turns_a_year_older_on_its_birthday_and_not_the_day_before()
	{
		// Eight years of birth dates, 1996 and 2000 among them, around each of ten seasons' birthdays
		var misses = new List<string>();
		for (var born = new DateOnly(1995, 1, 1); born.Year <= 2002; born = born.AddDays(1))
		{
			for (var season = 2026; season < 2036; season++)
			{
				var years = season - born.Year;
				var birthday = born.AddYears(years);
				var before = Player.AgeOn(born, birthday.AddDays(-1));
				var on = Player.AgeOn(born, birthday);
				if (before != years - 1 || on != years)
				{
					misses.Add($"born {born}: {before} the day before {birthday}, {on} on it");
				}
			}
		}

		misses.ShouldBeEmpty();
	}
}
