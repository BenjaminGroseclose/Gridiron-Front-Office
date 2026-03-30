using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application.Interfaces;

public interface ILeagueWizardService
{
	/// <summary>
	/// Creates a default league with the specified name.
	/// </summary>
	/// <param name="leagueName">The name of the league to create</param>
	/// <param name="startYear">The start year of the league</param>
	Task CreateDefaultLeagueAsync(string leagueName, int startYear);

	/// <summary>
	/// Returns a list of default teams that can be used when creating a new league.
	/// </summary>
	/// <returns>A collection of default teams.</returns>
	Task<IEnumerable<Team>> GetDefaultTeams();

	/// <summary>
	/// Creates a new league with settings
	/// </summary>
	Task CreateLeagueAsync(League league, int? userTeamId);
}