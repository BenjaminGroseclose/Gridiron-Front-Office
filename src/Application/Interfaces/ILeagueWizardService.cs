namespace GridironFrontOffice.Application.Interfaces;

public interface ILeagueWizardService
{
	/// <summary>
	/// Creates a default league with the specified name.
	/// </summary>
	/// <param name="leagueName">The name of the league to create</param>
	Task CreateDefaultLeagueAsync(string leagueName);

}