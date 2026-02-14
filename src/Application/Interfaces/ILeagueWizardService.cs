using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Forms;

namespace GridironFrontOffice.Application.Interfaces;

public interface ILeagueWizardService
{
	/// <summary>
	/// The current league setup data being configured
	/// </summary>
	LeagueSetupForm CurrentLeagueSetupForm { get; }

	/// <summary>
	/// Whether the league can be created with the current settings and selections
	/// This should validate that all required fields are filled out and that the selections are valid
	/// </summary>
	bool CanCreateLeague { get; }

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
	/// Updates the league setup form with new data and triggers state change notifications
	/// </summary>
	/// <param name="updatedForm">A partial league setup form containing the updated values</param>
	void UpdateLeagueSetupForm(LeagueSetupForm updatedForm);

	/// <summary>
	/// Selects the data configuration for the league, either default seed data or custom data from a JSON file
	/// </summary>
	/// <param name="isDefault">Whether to use the default seed data or custom data</param>
	/// <param name="jsonFilePath">The path to the JSON file containing custom data, if applicable</param>
	void SelectDataConfiguration(bool isDefault, string? jsonFilePath = null);

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