using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application.Interfaces;

public interface IScheduleService
{
	Task<IEnumerable<Game>> GetSchedule(int seasonID);
	Task<IEnumerable<Game>> GetScheduleForWeek(int seasonID, int week);
	Task<IEnumerable<Game>> GetScheduleForTeam(int seasonID, int teamID);

	/// <summary>
	/// Generates a schedule for the given season.
	/// </summary>
	/// <param name="seasonID">The season ID for which to generate the schedule.</param>
	/// <returns>True if the schedule was successfully created, otherwise false.</returns>
	Task<bool> CreateFreshSchedule(int seasonID);

	/// <summary>
	/// Generates a schedule for the given season based on the previous season's schedule. Logic should attempt to follow
	/// real rules for schedule generation, such as divisional matchups, conference matchups, and inter-conference matchups.
	/// </summary>
	/// <param name="seasonID">The season ID for which to generate the schedule.</param>
	/// <param name="previousSeasonID">The previous season ID to base the schedule on.</param>
	/// <returns>True if the schedule was successfully created, otherwise false.</returns>
	Task<bool> CreateScheduleFromPreviousSeason(int seasonID, int previousSeasonID);
}