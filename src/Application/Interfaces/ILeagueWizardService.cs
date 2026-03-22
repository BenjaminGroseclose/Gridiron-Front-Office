using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application.Interfaces;

public interface ILeagueWizardService
{
	/// <summary>
	/// Creates a default league with the specified name.
	/// </summary>
	/// <param name="leagueName">The name of the league to create</param>
	Task CreateDefaultLeagueAsync(string leagueName);

	/// <summary>
	/// Returns a list of default teams that can be used when creating a new league.
	/// </summary>
	/// <returns>A collection of default teams.</returns>
	Task<IEnumerable<Team>> GetDefaultTeams();

	/// <summary>
	/// Creates a new league with settings
	/// </summary>
	Task CreateLeagueAsync(string leagueName, IEnumerable<Team> teams, double? salaryCapFloor);
}