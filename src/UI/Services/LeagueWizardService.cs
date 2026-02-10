using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Domain.Forms;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.UI.Services;

/// <summary>
/// Service responsible for setting up a new league
/// </summary>
public class LeagueSetupService : ILeagueWizardService
{
	private readonly ISeedDataService _seedDataService;
	private LeagueSetupForm _leagueSetupForm;
	private int _currentStep = 0;
	private const int TOTAL_STEPS = 3;
	private int[] _completedSteps = Array.Empty<int>();
	private IEnumerable<Team> _availableTeams = [];
	private IEnumerable<Conference> _availableConferences = [];
	private IEnumerable<Division> _availableDivisions = [];
	private IEnumerable<Stadium> _availableStadiums = [];

	public event Action? OnChange;

	public LeagueSetupForm CurrentLeagueSetupForm => _leagueSetupForm;

	public LeagueSetupService(ISeedDataService seedDataService)
	{
		_seedDataService = seedDataService;
		_leagueSetupForm = new LeagueSetupForm();
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
					return !string.IsNullOrWhiteSpace(_leagueSetupForm.LeagueName)
						&& !string.IsNullOrWhiteSpace(_leagueSetupForm.CoachName)
						&& _leagueSetupForm.CoachDateOfBirth != default;
				case 1:
					return _leagueSetupForm.SelectedTeamID > 0;
				case 2:
					return !string.IsNullOrWhiteSpace(_leagueSetupForm.LeagueName);
				default:
					return false;
			}
		}
	}

	public IEnumerable<Team> AvailableTeams => _availableTeams;

	public void FinalizeLeagueSetup()
	{
		throw new NotImplementedException();
	}

	public void ResetLeagueSetup()
	{
		_leagueSetupForm = new LeagueSetupForm();
		NotifyStateChanged();
	}

	public void UpdateProfile(string leagueName, string coachName, DateTime? coachDateOfBirth, LeagueDifficultyLevel leagueDifficulty)
	{
		_leagueSetupForm.LeagueName = leagueName;
		_leagueSetupForm.CoachName = coachName;
		if (coachDateOfBirth.HasValue)
		{
			_leagueSetupForm.CoachDateOfBirth = coachDateOfBirth.Value;
		}
		_leagueSetupForm.LeagueDifficulty = leagueDifficulty;
		NotifyStateChanged();
	}

	public void UpdateLeagueSettings(bool injuriesEnabled, int numOfPlayoffsTeams, int numOfRegularSeasonGames, int rosterSize, int practiceSquadSize, bool canBeFired)
	{
		_leagueSetupForm.InjuriesEnabled = injuriesEnabled;
		_leagueSetupForm.NumOfPlayoffsTeams = numOfPlayoffsTeams;
		_leagueSetupForm.NumOfRegularSeasonGames = numOfRegularSeasonGames;
		_leagueSetupForm.RosterSize = rosterSize;
		_leagueSetupForm.PracticeSquadSize = practiceSquadSize;
		_leagueSetupForm.CanBeFired = canBeFired;
		_completedSteps = _completedSteps.Append(_currentStep).Distinct().ToArray();
		NotifyStateChanged();
	}

	public void UpdateSelectedTeam(int teamID)
	{
		_leagueSetupForm.SelectedTeamID = teamID;
		_completedSteps = _completedSteps.Append(_currentStep).Distinct().ToArray();
		NotifyStateChanged();
	}


	public void GoToNextStep()
	{
		if (_currentStep < TOTAL_STEPS - 1) // Assuming there are 3 steps indexed 0-2
		{
			_currentStep++;
			NotifyStateChanged();
		}
	}

	public void GoToPreviousStep()
	{
		if (_currentStep > 0)
		{
			_currentStep--;
			NotifyStateChanged();
		}
	}


	/// <summary>
	/// Gets the list of available teams for selection
	/// </summary>
	public IEnumerable<Team> GetAvailableTeams() => _availableTeams;

	/// <summary>
	/// Gets the list of available conferences for selection
	/// </summary>
	public IEnumerable<Conference> GetConferences() => _availableConferences;

	/// <summary>
	/// Gets the list of available divisions for selection
	/// </summary>
	public IEnumerable<Division> GetDivisions() => _availableDivisions;

	public IEnumerable<Stadium> GetStadiums() => _availableStadiums;
}