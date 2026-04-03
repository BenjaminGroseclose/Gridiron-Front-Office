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
	private readonly IBaseRepository<Conference> _conferenceRepository;
	private readonly IBaseRepository<Division> _divisionRepository;
	private readonly IBaseRepository<Player> _playerRepository;
	private readonly IBaseRepository<Stadium> _stadiumRepository;
	private readonly IBaseRepository<League> _leagueRepository;
	private readonly ISeedDataService _seedDataService;
	private readonly IPlayerGeneratorService _playerGeneratorService;

	public LeagueSetupService(GameManager gameManager,
		IBaseRepository<Team> teamRepository,
		IBaseRepository<Conference> conferenceRepository,
		IBaseRepository<Division> divisionRepository,
		IBaseRepository<Player> playerRepository,
		IBaseRepository<Stadium> stadiumRepository,
		IBaseRepository<League> leagueRepository,
		ISeedDataService seedDataService,
		IPlayerGeneratorService playerGeneratorService)
	{
		_gameManager = gameManager;
		_teamRepository = teamRepository;
		_conferenceRepository = conferenceRepository;
		_divisionRepository = divisionRepository;
		_stadiumRepository = stadiumRepository;
		_leagueRepository = leagueRepository;
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
			var playersToGenerate = await _playerGeneratorService.GeneratePlayersForTeamAsync(team.TeamID, startYear);
			await _playerRepository.BulkInsertAsync(playersToGenerate);
		}
	}

	public async Task CreateLeagueAsync(League league, int? userTeamId)
	{
		// Step 1: Create League Settings
		await _leagueRepository.InsertAsync(league);

		// Step 2: Create Schedule / Games


	}


	public async Task<IEnumerable<Team>> GetDefaultTeams() => await this._teamRepository.GetAllAsync();
}