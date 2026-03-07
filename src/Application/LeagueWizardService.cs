using System.Runtime.InteropServices;
using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Forms;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.Application;

/// <summary>
/// Service responsible for setting up a new league
/// </summary>
public class LeagueSetupService : ILeagueWizardService
{
	private readonly GameManager _gameManager;
	private readonly IBaseRepository<Team> _teamRepository;
	private readonly IBaseRepository<Conference> _conferenceRepository;
	private readonly IBaseRepository<Division> _divisionRepository;
	private readonly IBaseRepository<Player> _playerRepository;
	private readonly IBaseRepository<Stadium> _stadiumRepository;
	private readonly ISeedDataService _seedDataService;
	private readonly IPlayerGeneratorService _playerGeneratorService;

	// Private fields to hold the state of the league setup process
	private LeagueSetupForm _leagueSetupForm;
	private int _currentStep = 0;
	private const int TOTAL_STEPS = 3;
	private int[] _completedSteps = Array.Empty<int>();
	private bool? _isDefaultDataSelected = null;
	private bool _loadingLeague = false;
	private bool _leagueCreated = false;

	public event Action? OnChange;

	public LeagueSetupForm CurrentLeagueSetupForm => _leagueSetupForm;

	public LeagueSetupService(GameManager gameManager,
		IBaseRepository<Team> teamRepository,
		IBaseRepository<Conference> conferenceRepository,
		IBaseRepository<Division> divisionRepository,
		IBaseRepository<Player> playerRepository,
		IBaseRepository<Stadium> stadiumRepository,
		ISeedDataService seedDataService,
		IPlayerGeneratorService playerGeneratorService)
	{
		_gameManager = gameManager;
		_teamRepository = teamRepository;
		_conferenceRepository = conferenceRepository;
		_divisionRepository = divisionRepository;
		_stadiumRepository = stadiumRepository;
		_playerRepository = playerRepository;
		_seedDataService = seedDataService;
		_playerGeneratorService = playerGeneratorService;
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

	public bool LoadingLeague => _loadingLeague && !_leagueCreated;

	public async Task GoToNextStepAsync()
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

				NotifyStateChanged();
			}
		}

		if (_currentStep == 0)
		{
			if (string.IsNullOrEmpty(CurrentLeagueSetupForm.LeagueName))
			{
				throw new DomainException("League name should have been set prior to proceeding to the next step. This should have been validated on the UI side.");
			}

			await this.InitializeLeagueDataAsync();

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
		_isDefaultDataSelected = isDefault;
		CurrentLeagueSetupForm.UsingDefaultData = isDefault;
		NotifyStateChanged();

		// TODO: Implement logic to load custom data from JSON file if isDefault is false and jsonFilePath is provided
	}

	public async Task<IEnumerable<Team>> GetTeamsForSelection()
	{
		if (!_leagueCreated)
		{
			return Enumerable.Empty<Team>(); // Return an empty list if the league has not been created yet
		}

		var teams = await _teamRepository.GetAllAsync();

		return teams;
	}

	private async Task InitializeLeagueDataAsync()
	{
		// Step 1: Start loading
		_loadingLeague = true;
		NotifyStateChanged();

		if (CurrentLeagueSetupForm.LeagueName == null)
		{
			throw new DomainException("League name must be set before initializing league data.");
		}

		// Step 2: Create Game Save and Initialize Database Schema
		_gameManager.CreateNewGame(CurrentLeagueSetupForm.LeagueName);

		// Step 3: Load Seed Data into Database
		// TODO: Handle JSON seed data loading if the user selected custom data configuration

		var (teams, stadiums, conferences, divisions) = await _seedDataService.LoadDefaultDataAsync();

		// Load stadiums, conferences, divisions, and teams first since players depend on teams existing in the database
		await _stadiumRepository.BulkInsertAsync(stadiums);
		await _conferenceRepository.BulkInsertAsync(conferences);
		await _divisionRepository.BulkInsertAsync(divisions);
		await _teamRepository.BulkInsertAsync(teams);

		// Step 4: Generate Players for each team based on the selected roster size and practice squad size
		var allTeams = await _teamRepository.GetAllAsync();

		foreach (var team in allTeams)
		{
			var playersToGenerate = await _playerGeneratorService.GeneratePlayersForTeamAsync(team.TeamID);
			await _playerRepository.BulkInsertAsync(playersToGenerate);
		}

		_loadingLeague = false;
		_leagueCreated = true;
		NotifyStateChanged();
	}
}