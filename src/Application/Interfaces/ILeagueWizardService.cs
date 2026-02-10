using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Domain.Forms;

namespace GridironFrontOffice.Application.Interfaces;

public interface ILeagueWizardService
{
	/// <summary>
	/// The current league setup data being configured
	/// </summary>
	LeagueSetupForm CurrentLeagueSetupForm { get; }

	/// <summary>
	/// The current step in the league setup process, indexed from 0
	/// </summary>
	int CurrentStep { get; }

	/// <summary>
	/// The steps that have been completed in the league setup process
	/// </summary>
	int[] CompletedSteps { get; }

	/// <summary>
	/// Whether the user can proceed to the next step in the league setup process
	/// </summary>
	bool CanProceedToNextStep { get; }

	/// <summary>
	/// Event triggered when the state of the wizard changes
	/// </summary>
	event Action OnChange;

	/// <summary>>
	/// Navigates to the next step in the league setup process
	/// </summary>
	void GoToNextStep();

	/// <summary>
	/// Navigates to the previous step in the league setup process
	/// </summary>
	void GoToPreviousStep();

	/// <summary>
	/// Updates the league settings with the provided league setting data
	/// </summary>
	void UpdateLeagueSettings(bool injuriesEnabled, int numOfPlayoffsTeams, int numOfRegularSeasonGames, int rosterSize, int practiceSquadSize, bool canBeFired);
	void UpdateSelectedTeam(int teamID);
	void UpdateProfile(string leagueName, string playerName, DateTime? playerDateOfBirth, LeagueDifficultyLevel leagueDifficulty);
	void ResetLeagueSetup();
	void FinalizeLeagueSetup();

	/// <summary>
	/// Gets the list of available teams for selection
	/// </summary>
	IEnumerable<Team> GetAvailableTeams();

	/// <summary>
	/// Gets the list of available conferences for selection
	/// </summary>
	IEnumerable<Conference> GetConferences();

	/// <summary>
	/// Gets the list of available divisions for selection
	/// </summary>
	IEnumerable<Division> GetDivisions();

	/// <summary>
	/// Gets the list of available stadiums for selection
	/// </summary>
	IEnumerable<Stadium> GetStadiums();
}