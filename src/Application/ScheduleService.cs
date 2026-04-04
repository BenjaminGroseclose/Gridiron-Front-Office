using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application;

public class ScheduleService : IScheduleService
{
	/// <summary>
	/// This is the list of weeks where team can have a bye week.
	/// </summary>
	private readonly int[] BYE_WEEKS = new int[] { 5, 10 }; // Example bye weeks for a 16-week season

	private readonly IBaseRepository<Game> _gameRepository;
	private readonly IBaseRepository<Team> _teamRepository;
	private readonly IBaseRepository<LeagueSetting> _leagueSettingRepository;
	private readonly ILogger<ScheduleService> _logger;

	public ScheduleService(IBaseRepository<Game> gameRepository, IBaseRepository<Team> teamRepository, IBaseRepository<LeagueSetting> leagueSettingRepository, ILogger<ScheduleService> logger)
	{
		_gameRepository = gameRepository;
		_teamRepository = teamRepository;
		_leagueSettingRepository = leagueSettingRepository;
		_logger = logger;
	}

	public async Task<bool> CreateFreshSchedule(int seasonID)
	{

	}

	public Task<bool> CreateScheduleFromPreviousSeason(int seasonID, int previousSeasonID)
	{
		var leagueSettings = (await _leagueSettingRepository.GetAllAsync()).FirstOrDefault(ls => ls.SeasonID == seasonID);

		if (leagueSettings == null)
		{
			throw new Exception($"No league settings found for season {seasonID}");
		}

		var numberOfWeeks = leagueSettings.NumOfRegularSeasonWeeks;

		var teams = await _teamRepository.GetAllAsync();

		if (teams == null || teams.Count() < 2)
		{
			throw new Exception($"Not enough teams to create a schedule for season {seasonID}. At least 2 teams are required.");
		}

		var games = new List<Game>();

		for (int week = 1; week <= numberOfWeeks; week++)
		{
			var gamesForWeek = new List<Game>();

			// Example placeholder game:
			var game = new Game
			{
				SeasonID = seasonID,
				WeekID = week,
				HomeTeamID = 1, // Placeholder team ID
				AwayTeamID = 2, // Placeholder team ID
				GameDate = DateTime.Now.AddDays(week * 7) // Placeholder game date
			};

			games.AddRange(gamesForWeek);

			_logger.LogInformation($"Created {gamesForWeek.Count} games for week {week} of season {seasonID}");
		}

		_logger.LogInformation($"Created {games.Count} games for season {seasonID}");

		await _gameRepository.BulkInsertAsync(games);
	}
	public async Task<IEnumerable<Game>> GetSchedule(int seasonID)
	{
		var games = await _gameRepository.GetAllAsync();
		return games.Where(g => g.SeasonID == seasonID).ToList();
	}

	public async Task<IEnumerable<Game>> GetScheduleForTeam(int seasonID, int teamID)
	{
		var games = await _gameRepository.GetAllAsync();
		return games.Where(g => g.SeasonID == seasonID && (g.HomeTeamID == teamID || g.AwayTeamID == teamID)).OrderBy(g => g.GameDate).ToList();
	}

	public async Task<IEnumerable<Game>> GetScheduleForWeek(int seasonID, int weekID)
	{
		var games = await _gameRepository.GetAllAsync();
		return games.Where(g => g.SeasonID == seasonID && g.WeekID == weekID).OrderBy(g => g.GameDate).ToList();
	}
}