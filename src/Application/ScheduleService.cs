using System.Security;
using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
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

	public async Task<bool> CreateScheduleFromPreviousSeason(int seasonID, int previousSeasonID)
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
		return true;
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

	private List<Matchup> GenerateOpponents(Team team, IEnumerable<Team> allTeams, int season)
	{
		var matchups = new List<Matchup>();

		// 1. Divisional matchups (play each team in the same division twice)
		var divisionalOpponents = allTeams.Where(t => t.Division == team.Division && t.TeamID != team.TeamID).ToList();
		foreach (var opponent in divisionalOpponents)
		{
			matchups.Add(new Matchup { OpponentID = opponent.TeamID, IsHome = true });
			matchups.Add(new Matchup { OpponentID = opponent.TeamID, IsHome = false }); // Play twice
		}

		// 2. Intra-conference matchups
		// Example: 2026 AFC East plays AFC West
		int intraConferenceDivisionID = (team.DivisionID + 1) % 4; // Simple rotation for example 

		return matchups;
	}

	private Division GetIntraRotation(Division division, int season)
	{
		return (division, season % 3) switch
		{
			(Division.North, 2024) => Division.South,
			(Division.North, 2025) => Division.East,
			(Division.North, 2026) => Division.West,
			(Division.South, 2024) => Division.East,
			(Division.South, 2025) => Division.West,
			(Division.South, 2026) => Division.North,
			(Division.East, 2024) => Division.West,
			(Division.East, 2025) => Division.North,
			(Division.East, 2026) => Division.South,
			(Division.West, 2024) => Division.North,
			(Division.West, 2025) => Division.South,
			(Division.West, 2026) => Division.East,
			_ => throw new Exception($"No intra-conference rotation defined for division {division} and season {season}")
		};
	}

	private class Matchup
	{
		public int OpponentID { get; set; }
		public bool IsHome { get; set; }
	}

}