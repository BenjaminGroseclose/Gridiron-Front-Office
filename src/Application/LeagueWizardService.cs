using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
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
	private readonly IBaseRepository<Player> _playerRepository;
	private readonly IBaseRepository<LeagueSetting> _leagueSettingRepository;
	private readonly ISeedDataService _seedDataService;
	private readonly IPlayerGeneratorService _playerGeneratorService;
	private readonly IScheduleService _scheduleService;

	public LeagueSetupService(GameManager gameManager,
		IBaseRepository<Team> teamRepository,
		IBaseRepository<Player> playerRepository,
		IBaseRepository<LeagueSetting> leagueSettingRepository,
		ISeedDataService seedDataService,
		IPlayerGeneratorService playerGeneratorService,
		IScheduleService scheduleService)
	{
		_gameManager = gameManager;
		_teamRepository = teamRepository;
		_leagueSettingRepository = leagueSettingRepository;
		_scheduleService = scheduleService;
		_playerRepository = playerRepository;
		_seedDataService = seedDataService;
		_playerGeneratorService = playerGeneratorService;
	}

	public async Task CreateDefaultLeagueAsync(string leagueName, int startYear)
	{
		if (leagueName == null)
		{
			throw new DomainException("League name must be set before initializing league data.");
		}

		// Step 2: Create Game Save and Initialize Database Schema
		_gameManager.CreateNewGame(leagueName);

		// Step 3: Load Seed Data into Database
		// TODO: Handle JSON seed data loading if the user selected custom data configuration
		await _seedDataService.LoadDefaultDataAsync(startYear);

		// Step 4: Generate Players for each team based on the selected roster size and practice squad size
		var allTeams = await _teamRepository.GetAllAsync();

		foreach (var team in allTeams)
		{
			var playersToGenerate = await _playerGeneratorService.GeneratePlayersForTeamAsync(team.TeamID, startYear);
			await _playerRepository.BulkInsertAsync(playersToGenerate);
		}
	}

	public async Task CreateLeagueAsync(LeagueSetting league, int? userTeamId)
	{
		// Step 1: Create League Settings
		await _leagueSettingRepository.InsertAsync(league);

		// Step 2: Create Season and Weeks
		await _scheduleService.StartSeason(league.SeasonID, league.NumOfRegularSeasonWeeks);

		// Step 3: Create Schedule / Games
		// Pass -1 to indicate that there is no previous season to base the schedule on
		await _scheduleService.CreateScheduleFromPreviousSeason(league.SeasonID, -1);

	}


	public async Task<IEnumerable<Team>> GetDefaultTeams() => await this._teamRepository.GetAllAsync();
}