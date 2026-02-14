using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Forms;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.UI.Services;

/// <summary>
/// Service responsible for setting up a new league
/// </summary>
public class LeagueSetupService : ILeagueWizardService
{
	private readonly ISeedDataService _seedDataService;
	// Private fields to hold the state of the league setup process
	private LeagueSetupForm _leagueSetupForm;
	private int _currentStep = 0;
	private const int TOTAL_STEPS = 3;
	private int[] _completedSteps = Array.Empty<int>();
	private IEnumerable<Team> _availableTeams = [];
	private IEnumerable<Conference> _availableConferences = [];
	private IEnumerable<Division> _availableDivisions = [];
	private IEnumerable<Stadium> _availableStadiums = [];
	private bool? _isDefaultDataSelected = null;

	public event Action? OnChange;

	public LeagueSetupForm CurrentLeagueSetupForm => _leagueSetupForm;

	public LeagueSetupService(ISeedDataService seedDataService)
	{
		_seedDataService = seedDataService;
		_leagueSetupForm = new LeagueSetupForm()
		{
			CoachName = string.Empty,
			CoachExperience = "Rookie",
			LeagueName = string.Empty,
			RosterSize = 53,
			PracticeSquadSize = 16,
			InjuriesEnabled = true,
			SalaryCap = 255,
			SalaryCapFloor = 0.9,
			StartingYear = 2025
		};
		LoadDataAsync();
	}

	/// <summary>
	/// Loads the available teams from seed data
	/// </summary>
	private async void LoadDataAsync()
	{
		try
		{
			var (teams, stadiums, conferences, divisions) = await _seedDataService.LoadDefaultDataAsync();
			_availableTeams = teams;
			_availableStadiums = stadiums;
			_availableConferences = conferences;
			_availableDivisions = divisions;
			NotifyStateChanged();

		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to load teams: {ex.Message}");
		}
	}

	private void NotifyStateChanged() => OnChange?.Invoke();

	public int CurrentStep => _currentStep;

	public int[] CompletedSteps => _completedSteps;

	public bool CanProceedToNextStep
	{
		get
		{
			switch (_currentStep)
			{
				case 0:
					return !string.IsNullOrWhiteSpace(_leagueSetupForm.CoachName) && !string.IsNullOrWhiteSpace(_leagueSetupForm.CoachExperience);
				case 1:
					return _leagueSetupForm.RosterSize.HasValue &&
							_leagueSetupForm.PracticeSquadSize.HasValue &&
							_leagueSetupForm.InjuriesEnabled.HasValue &&
							_leagueSetupForm.SalaryCap.HasValue &&
							_leagueSetupForm.SalaryCapFloor.HasValue &&
							_leagueSetupForm.StartingYear.HasValue;
				case 2:
					return _isDefaultDataSelected.HasValue; // Ensure the user has selected a data configuration option
				default:
					return false;
			}
		}
	}

	public bool CanCreateLeague => _leagueSetupForm.IsValid();

	public void GoToNextStep()
	{
		// Check if Form is complete for the current step before allowing navigation to the next step
		if (_currentStep == 2)
		{
			if (!CanCreateLeague)
			{
				return; // Prevent proceeding to the next step if the form is not valid
			}
			else
			{

			}
		}

		if (_currentStep < TOTAL_STEPS - 1) // Assuming there are 3 steps indexed 0-2
		{
			_currentStep++;
			_completedSteps = _completedSteps.Append(_currentStep - 1).ToArray(); // Mark the previous step as completed
			NotifyStateChanged();
		}
	}

	public void GoToPreviousStep()
	{
		if (_currentStep > 0)
		{
			_currentStep--;
			_completedSteps = _completedSteps.Where(step => step < _currentStep).ToArray(); // Remove the current step from completed steps
			NotifyStateChanged();
		}
	}

	public IEnumerable<Team> GetAvailableTeams() => _availableTeams;

	public IEnumerable<Conference> GetConferences() => _availableConferences;

	public IEnumerable<Division> GetDivisions() => _availableDivisions;

	public IEnumerable<Stadium> GetStadiums() => _availableStadiums;

	public void UpdateLeagueSetupForm(LeagueSetupForm updatedForm)
	{
		var form = CurrentLeagueSetupForm;

		form.CoachName = updatedForm.CoachName ?? form.CoachName;
		form.CoachExperience = updatedForm.CoachExperience ?? form.CoachExperience;
		form.LeagueName = updatedForm.LeagueName ?? form.LeagueName;
		form.RosterSize = updatedForm.RosterSize ?? form.RosterSize;
		form.PracticeSquadSize = updatedForm.PracticeSquadSize ?? form.PracticeSquadSize;
		form.InjuriesEnabled = updatedForm.InjuriesEnabled ?? form.InjuriesEnabled;
		form.SalaryCap = updatedForm.SalaryCap ?? form.SalaryCap;
		form.SalaryCapFloor = updatedForm.SalaryCapFloor ?? form.SalaryCapFloor;
		form.StartingYear = updatedForm.StartingYear ?? form.StartingYear;
		NotifyStateChanged();
	}

	public void SelectDataConfiguration(bool isDefault, string? jsonFilePath = null)
	{
		this._isDefaultDataSelected = isDefault;

		// TODO: Implement logic to load custom data from JSON file if isDefault is false and jsonFilePath is provided
	}
}