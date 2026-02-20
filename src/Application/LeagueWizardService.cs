using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain.Forms;
using GridironFrontOffice.Persistence;

namespace GridironFrontOffice.Application.Services;

/// <summary>
/// Service responsible for setting up a new league
/// </summary>
public class LeagueSetupService : ILeagueWizardService
{
	private readonly GameManager _gameManager;

	// Private fields to hold the state of the league setup process
	private LeagueSetupForm _leagueSetupForm;
	private int _currentStep = 0;
	private const int TOTAL_STEPS = 3;
	private int[] _completedSteps = Array.Empty<int>();
	private bool? _isDefaultDataSelected = null;
	private bool _loadingLeague = false;

	public event Action? OnChange;

	public LeagueSetupForm CurrentLeagueSetupForm => _leagueSetupForm;

	public LeagueSetupService()
	{
		_gameManager = new GameManager();
		_leagueSetupForm = new LeagueSetupForm()
		{
			LeagueName = string.Empty,
			RosterSize = 53,
			PracticeSquadSize = 16,
			InjuriesEnabled = true,
			SalaryCap = 255,
			SalaryCapFloor = 0.9,
			StartingYear = 2025,
			SelectedTeamID = null
		};
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
					return _isDefaultDataSelected.HasValue; // Ensure the user has selected a data configuration option
				case 1:
					return _leagueSetupForm.RosterSize.HasValue &&
							_leagueSetupForm.PracticeSquadSize.HasValue &&
							_leagueSetupForm.InjuriesEnabled.HasValue &&
							_leagueSetupForm.SalaryCap.HasValue &&
							_leagueSetupForm.SalaryCapFloor.HasValue &&
							_leagueSetupForm.StartingYear.HasValue;
				case 2:
					return _leagueSetupForm.SelectedTeamID.HasValue; // Ensure the user has selected a team
				default:
					return false;
			}
		}
	}

	public bool CanCreateLeague => _leagueSetupForm.IsValid();

	public bool LoadingLeague => _loadingLeague;

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
				_loadingLeague = true; // Start loading league data based on the user's selections
				NotifyStateChanged();
			}
		}

		if (_currentStep == 0 && !string.IsNullOrEmpty(this.CurrentLeagueSetupForm.LeagueName))
		{
			this._gameManager.CreateNewGame(this.CurrentLeagueSetupForm.LeagueName);
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

	public void UpdateLeagueSetupForm(LeagueSetupForm updatedForm)
	{
		var form = CurrentLeagueSetupForm;

		form.LeagueName = updatedForm.LeagueName ?? form.LeagueName;
		form.RosterSize = updatedForm.RosterSize ?? form.RosterSize;
		form.PracticeSquadSize = updatedForm.PracticeSquadSize ?? form.PracticeSquadSize;
		form.InjuriesEnabled = updatedForm.InjuriesEnabled ?? form.InjuriesEnabled;
		form.SalaryCap = updatedForm.SalaryCap ?? form.SalaryCap;
		form.SalaryCapFloor = updatedForm.SalaryCapFloor ?? form.SalaryCapFloor;
		form.StartingYear = updatedForm.StartingYear ?? form.StartingYear;
		form.UsingDefaultData = updatedForm.UsingDefaultData ?? form.UsingDefaultData;

		form.SelectedTeamID = updatedForm.SelectedTeamID ?? form.SelectedTeamID;

		NotifyStateChanged();
	}

	public void SelectDataConfiguration(bool isDefault, string? jsonFilePath = null)
	{
		this._isDefaultDataSelected = isDefault;
		CurrentLeagueSetupForm.UsingDefaultData = isDefault;
		NotifyStateChanged();

		// TODO: Implement logic to load custom data from JSON file if isDefault is false and jsonFilePath is provided
	}
}