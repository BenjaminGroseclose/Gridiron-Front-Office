using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Application.State;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence;
using GridironFrontOffice.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application;

/// <summary>
/// Service responsible for setting up a new league
/// </summary>
public class LeagueSetupService : ILeagueWizardService
{
	private readonly GameManager _gameManager;
	private readonly ITeamService _teamService;
	private readonly IBaseRepository<Player> _playerRepository;
	private readonly IBaseRepository<LeagueSetting> _leagueSettingRepository;
	private readonly ISeedDataService _seedDataService;
	private readonly IPlayerGeneratorService _playerGeneratorService;
	private readonly IScheduleService _scheduleService;
	private readonly ILogger<LeagueSetupService> _logger;
	private readonly AppState _appState;

	public LeagueSetupService(GameManager gameManager,
		ITeamService teamService,
		IBaseRepository<Player> playerRepository,
		IBaseRepository<LeagueSetting> leagueSettingRepository,
		ISeedDataService seedDataService,
		IPlayerGeneratorService playerGeneratorService,
		IScheduleService scheduleService,
		ILogger<LeagueSetupService> logger,
		AppState appState)
	{
		_gameManager = gameManager;
		_teamService = teamService;
		_leagueSettingRepository = leagueSettingRepository;
		_scheduleService = scheduleService;
		_logger = logger;
		_playerRepository = playerRepository;
		_seedDataService = seedDataService;
		_playerGeneratorService = playerGeneratorService;
		_appState = appState;
	}

	public async Task CreateDefaultLeagueAsync(string leagueName, int startYear)
	{
		try
		{
			if (leagueName == null)
			{
				throw new DomainException("League name must be set before initializing league data.");
			}

			// Step 1: Create Game Save and Initialize Database Schema
			_gameManager.CreateNewGame(leagueName);

			// Step 2: Load Seed Data into Database
			// TODO: Handle JSON seed data loading if the user selected custom data configuration
			await _seedDataService.LoadDefaultDataAsync(startYear);

			// Step 3: Generate Players for each team based on the selected roster size and practice squad size
			var allTeams = await _teamService.GetAllTeamsAsync();

			foreach (var team in allTeams)
			{
				var playersToGenerate = await _playerGeneratorService.GeneratePlayersForTeamAsync(team.TeamID, startYear);
				await _playerRepository.BulkInsertAsync(playersToGenerate);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating default league with name {LeagueName} and start year {StartYear}", leagueName, startYear);
			throw;
		}
	}

	public async Task CreateLeagueAsync(LeagueSetting league, int? userTeamID)
	{
		try
		{
			if (userTeamID == null)
			{
				throw new DomainException("User team ID must be set before initializing league data.");
			}

			// Step 1: Create League Settings
			await _leagueSettingRepository.InsertAsync(league);

			_logger.LogInformation("Created league settings for league {LeagueName} with season {SeasonID}", league.Name, league.SeasonID);

			// Step 2: Create Season and Weeks
			await _scheduleService.StartSeason(league.SeasonID, league.NumOfRegularSeasonWeeks);

			_logger.LogInformation("Started season and weeks for season {SeasonID}", league.SeasonID);

			// Step 3: Create Schedule / Games
			// Pass -1 to indicate that there is no previous season to base the schedule on
			await _scheduleService.CreateScheduleFromPreviousSeason(league.SeasonID, -1);

			_logger.LogInformation("Created schedule for season {SeasonID}", league.SeasonID);

			// Step 4: Set User Team to team. 
			var userTeam = await _teamService.GetTeam(userTeamID.Value);
			if (userTeam == null)
			{
				throw new DomainException($"User team with ID {userTeamID.Value} not found.");
			}

			userTeam.IsUserControlled = true;
			await _teamService.UpdateTeam(userTeam);

			_logger.LogInformation("Set user team with ID {UserTeamID} as user controlled", userTeamID.Value);

			// Step 5: Update App State with new league information
			var season = await _scheduleService.GetCurrentSeason();
			_appState.UpdateState(state => state with
			{
				UserTeamID = userTeamID,
				CurrentSeason = season,
				CurrentSavePath = _gameManager.CurrentDatabasePath,
				CurrentRoute = "/home",
				RouteHistory = new Stack<string>(),
				IsLoading = false,
				Error = null,
				CurrentDateTime = league.CurrentDate
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating league with season {Season}", league.SeasonID);
			throw;
		}
	}


	public async Task<IEnumerable<Team>> GetDefaultTeams() => await _teamService.GetAllTeamsAsync();
}