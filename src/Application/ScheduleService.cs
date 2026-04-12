using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application;

public class ScheduleService : IScheduleService
{
	/// <summary>
	/// The weeks during which teams are eligible to have a bye week.
	/// </summary>
	private readonly int[] BYE_WEEKS = new int[] { 5, 6, 7, 8, 9, 10, 11, 12 };

	private readonly IBaseRepository<Game> _gameRepository;
	private readonly IBaseRepository<Season> _seasonRepository;
	private readonly IBaseRepository<Week> _weekRepository;
	private readonly ITeamService _teamService;
	private readonly ILogger<ScheduleService> _logger;

	public ScheduleService(IBaseRepository<Game> gameRepository, ITeamService teamService, IBaseRepository<Season> seasonRepository, IBaseRepository<Week> weekRepository, ILogger<ScheduleService> logger)
	{
		_gameRepository = gameRepository;
		_teamService = teamService;
		_seasonRepository = seasonRepository;
		_weekRepository = weekRepository;
		_logger = logger;
	}

	// TODO: I think I need to create several years out for ContractYears to make sense
	public async Task<bool> StartSeason(int seasonID, int numberOfWeeks)
	{
		var season = await _seasonRepository.GetByIDAsync(seasonID);

		if (season == null)
		{
			throw new ArgumentException($"Season with ID {seasonID} does not exist.");
		}

		season.Status = SeasonStatus.PreSeason;
		season.IsCurrentSeason = true;
		season.UpdateDate = DateTimeOffset.UtcNow;

		await _seasonRepository.UpdateAsync(season);

		var previousSeason = await _seasonRepository.GetByIDAsync(seasonID - 1);

		if (previousSeason != null)
		{
			// Mark previous season as not current
			previousSeason.IsCurrentSeason = false;
			previousSeason.Status = SeasonStatus.Completed;
			await _seasonRepository.UpdateAsync(previousSeason);
		}
		else
		{
			_logger.LogInformation($"No previous season found for seasonID {seasonID - 1}. This may be the first season.");
		}


		for (int weekNum = 1; weekNum <= numberOfWeeks; weekNum++)
		{
			var week = new Week
			{
				SeasonID = seasonID,
				Name = $"Week {weekNum}",
				Type = WeekType.RegularSeason
			};

			await _weekRepository.InsertAsync(week);
		}

		return true;
	}

	public async Task<bool> CreateScheduleFromPreviousSeason(int seasonID, int previousSeasonID)
	{
		var currentSeason = await _seasonRepository.GetByIDAsync(seasonID);

		if (currentSeason == null)
		{
			throw new ArgumentException($"Season with ID {seasonID} does not exist.");
		}

		var weeks = (await _weekRepository.GetAllAsync())
			.Where(w => w.SeasonID == seasonID)
			.OrderBy(w => w.WeekID)
			.ToList();

		var teams = (await _teamService.GetAllTeamsAsync()).ToList();

		if (teams.Count < 2)
		{
			throw new Exception($"Not enough teams to create a schedule for season {seasonID}. At least 2 teams are required.");
		}

		// Build divisional rank lookups from previous season (or random fallback for first season)
		var divisionalRankings = await BuildDivisionalRankings(teams, previousSeasonID, seasonID);
		var divisionTeamsByRank = BuildDivisionTeamsByRank(teams, divisionalRankings);

		// Generate every team's matchup list and deduplicate into unique games
		var seen = new HashSet<(int, int)>();
		var rawGames = new List<Game>();

		foreach (var team in teams)
		{
			var matchups = GenerateOpponents(team, teams, seasonID, divisionalRankings, divisionTeamsByRank);

			foreach (var matchup in matchups)
			{
				int homeID = matchup.IsHome ? team.TeamID : matchup.OpponentID;
				int awayID = matchup.IsHome ? matchup.OpponentID : team.TeamID;

				if (seen.Contains((homeID, awayID)) || seen.Contains((awayID, homeID)))
					continue;

				seen.Add((homeID, awayID));
				rawGames.Add(new Game
				{
					SeasonID = seasonID,
					HomeTeamID = homeID,
					AwayTeamID = awayID
				});
			}
		}

		// Assign games to weeks and set dates
		var games = AssignGamesToWeeks(rawGames, teams, weeks, currentSeason);

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
		return games.Where(g => g.SeasonID == seasonID && (g.HomeTeamID == teamID || g.AwayTeamID == teamID)).OrderBy(g => g.GameDateTime).ToList();
	}

	public async Task<IEnumerable<Game>> GetScheduleForWeek(int seasonID, int weekID)
	{
		var games = await _gameRepository.GetAllAsync();
		return games.Where(g => g.SeasonID == seasonID && g.WeekID == weekID).OrderBy(g => g.GameDateTime).ToList();
	}

	// ---------------------------------------------------------------------------
	// Rank lookup helpers
	// ---------------------------------------------------------------------------

	/// <summary>
	/// Builds a teamID → divisional rank (1-based) dictionary from previous season standings.
	/// Falls back to a seeded random shuffle per division when no standings exist (e.g. first season).
	/// </summary>
	private async Task<Dictionary<int, int>> BuildDivisionalRankings(
		List<Team> teams, int previousSeasonID, int currentSeasonID)
	{
		var rankings = new Dictionary<int, int>();

		var conferences = new[] { Conference.AFC, Conference.NFC };
		var divisions = new[] { Division.North, Division.South, Division.East, Division.West };

		foreach (var conference in conferences)
		{
			foreach (var division in divisions)
			{
				var standings = await _teamService.GetTeamStandings(previousSeasonID, conference, division);

				if (standings.Count > 0)
				{
					// Standings are already ranked 1-based by CalculateStandings
					foreach (var standing in standings)
					{
						rankings[standing.Team.TeamID] = standing.Ranking;
					}
				}
				else
				{
					// No previous season data — assign random rank seeded by season for determinism
					var divTeams = teams
						.Where(t => t.Conference == conference && t.Division == division)
						.ToList();

					var rng = new Random(currentSeasonID ^ ((int)conference * 100) ^ ((int)division * 10));
					var shuffled = divTeams.OrderBy(_ => rng.Next()).ToList();

					for (int i = 0; i < shuffled.Count; i++)
					{
						rankings[shuffled[i].TeamID] = i + 1;
					}
				}
			}
		}

		return rankings;
	}

	/// <summary>
	/// Builds a (Conference, Division) → teams-sorted-by-rank dictionary for O(1) rank lookups.
	/// </summary>
	private static Dictionary<(Conference, Division), List<Team>> BuildDivisionTeamsByRank(
		List<Team> teams, Dictionary<int, int> divisionalRankings)
	{
		return teams
			.GroupBy(t => (t.Conference, t.Division))
			.ToDictionary(
				g => g.Key,
				g => g.OrderBy(t => divisionalRankings.GetValueOrDefault(t.TeamID, int.MaxValue)).ToList()
			);
	}

	// ---------------------------------------------------------------------------
	// Matchup generation
	// ---------------------------------------------------------------------------

	private List<Matchup> GenerateOpponents(
		Team team,
		IEnumerable<Team> allTeams,
		int season,
		Dictionary<int, int> divisionalRankings,
		Dictionary<(Conference, Division), List<Team>> divisionTeamsByRank)
	{
		var matchups = new List<Matchup>();
		int myRank = divisionalRankings.GetValueOrDefault(team.TeamID, 1);

		// 1. Divisional matchups — 3 opponents × 2 (home + away) = 6 games
		var divisionalOpponents = allTeams
			.Where(t => t.Division == team.Division && t.Conference == team.Conference && t.TeamID != team.TeamID)
			.ToList();

		foreach (var opponent in divisionalOpponents)
		{
			matchups.Add(new Matchup { OpponentID = opponent.TeamID, IsHome = true });
			matchups.Add(new Matchup { OpponentID = opponent.TeamID, IsHome = false });
		}

		// 2. Intra-conference rotation — all 4 teams from one same-conf division = 4 games
		Division intraRotation = GetIntraRotation(team.Division, season);
		var intraConferenceOpponents = allTeams
			.Where(t => t.Division == intraRotation && t.Conference == team.Conference)
			.ToList();

		for (int i = 0; i < intraConferenceOpponents.Count; i++)
		{
			// Alternate home/away within the group so half the matchups are home
			bool isHome = i % 2 == 0;
			matchups.Add(new Matchup { OpponentID = intraConferenceOpponents[i].TeamID, IsHome = isHome });
		}

		// 3. Inter-conference rotation — all 4 teams from one opposite-conf division = 4 games
		Division interRotation = GetInterRotation(team.Division, season);
		var interConferenceOpponents = allTeams
			.Where(t => t.Division == interRotation && t.Conference != team.Conference)
			.ToList();

		for (int i = 0; i < interConferenceOpponents.Count; i++)
		{
			bool isHome = i % 2 == 0;
			matchups.Add(new Matchup { OpponentID = interConferenceOpponents[i].TeamID, IsHome = isHome });
		}

		// 4. Same-rank intra-conference — 1 game each from the 2 remaining same-conf divisions = 2 games
		var allDivisions = new[] { Division.North, Division.South, Division.East, Division.West };
		var remainingIntraDivisions = allDivisions
			.Where(d => d != team.Division && d != intraRotation)
			.ToList();

		// Flip home/away assignment each season so teams alternate hosting
		bool firstIsHome = season % 2 == 0;

		for (int i = 0; i < remainingIntraDivisions.Count; i++)
		{
			var key = (team.Conference, remainingIntraDivisions[i]);
			if (!divisionTeamsByRank.TryGetValue(key, out var rankList))
				continue;

			// Find opponent at same rank; clamp in case division has fewer teams
			int rankIndex = Math.Min(myRank - 1, rankList.Count - 1);
			var opponent = rankList[rankIndex];

			bool isHome = i == 0 ? firstIsHome : !firstIsHome;
			matchups.Add(new Matchup { OpponentID = opponent.TeamID, IsHome = isHome });
		}

		// 5. 17th game — same rank, opposite conference, from the inter-conference rotation division
		var interKey = (team.Conference == Conference.AFC ? Conference.NFC : Conference.AFC, interRotation);
		if (divisionTeamsByRank.TryGetValue(interKey, out var interRankList))
		{
			int rankIndex = Math.Min(myRank - 1, interRankList.Count - 1);
			var seventeenth = interRankList[rankIndex];

			// Home/away alternates by season parity
			bool isHome = season % 2 == 0;
			matchups.Add(new Matchup { OpponentID = seventeenth.TeamID, IsHome = isHome });
		}

		return matchups;
	}

	// ---------------------------------------------------------------------------
	// Week / bye assignment
	// ---------------------------------------------------------------------------

	/// <summary>
	/// Assigns each game to a week, distributes byes evenly across BYE_WEEKS,
	/// and sets GameDate relative to the season start date.
	/// </summary>
	private List<Game> AssignGamesToWeeks(
		List<Game> games,
		List<Team> teams,
		List<Week> weeks,
		Season season)
	{
		// --- Assign bye weeks ---
		// Spread teams evenly across available bye weeks, alternating conferences
		var byeWeek = new Dictionary<int, int>(); // teamID → bye week number
		var teamsInByeOrder = teams
			.OrderBy(t => t.Conference)
			.ThenBy(t => t.Division)
			.ToList();

		for (int i = 0; i < teamsInByeOrder.Count; i++)
		{
			int week = BYE_WEEKS[i % BYE_WEEKS.Length];
			byeWeek[teamsInByeOrder[i].TeamID] = week;
		}

		// --- Track which teams are already scheduled each week ---
		// Index 0 = week 1, index totalWeeks-1 = last week
		var scheduledTeamsPerWeek = new HashSet<int>[weeks.Count + 1];
		for (int w = 1; w <= weeks.Count; w++)
			scheduledTeamsPerWeek[w] = new HashSet<int>();

		// --- Shuffle for variety (seeded for determinism per season) ---
		var rng = new Random(season.SeasonID);
		var shuffled = games.OrderBy(_ => rng.Next()).ToList();
		var seasonStartDate = season.RegularSeasonStartDate;

		// --- Greedy first-fit assignment ---
		foreach (var game in shuffled)
		{
			bool assigned = false;

			foreach (var week in weeks)

			{
				bool homeOnBye = byeWeek.TryGetValue(game.HomeTeamID, out int homeBye) && homeBye == week.WeekID;
				bool awayOnBye = byeWeek.TryGetValue(game.AwayTeamID, out int awayBye) && awayBye == week.WeekID;
				bool homeAlreadyPlaying = scheduledTeamsPerWeek[week.WeekID].Contains(game.HomeTeamID);
				bool awayAlreadyPlaying = scheduledTeamsPerWeek[week.WeekID].Contains(game.AwayTeamID);

				if (homeOnBye || awayOnBye || homeAlreadyPlaying || awayAlreadyPlaying)
				{
					continue;
				}

				game.WeekID = week.WeekID;
				scheduledTeamsPerWeek[week.WeekID].Add(game.HomeTeamID);
				scheduledTeamsPerWeek[week.WeekID].Add(game.AwayTeamID);
				assigned = true;
				break;
			}

			if (!assigned)
			{
				throw new DomainException(
						$"Could not assign a week slot for game HomeTeam={game.HomeTeamID} AwayTeam={game.AwayTeamID} in season {game.SeasonID}. " +
						$"Game scheduled to week 1 as fallback.", "SCHEDULING_ERROR");
			}
		}

		return shuffled;
	}

	// ---------------------------------------------------------------------------
	// Rotation tables
	// ---------------------------------------------------------------------------

	private Division GetIntraRotation(Division division, int season)
	{
		return (division, season % 3) switch
		{
			(Division.North, 0) => Division.South,
			(Division.North, 1) => Division.East,
			(Division.North, 2) => Division.West,
			(Division.South, 0) => Division.East,
			(Division.South, 1) => Division.West,
			(Division.South, 2) => Division.North,
			(Division.East, 0) => Division.West,
			(Division.East, 1) => Division.North,
			(Division.East, 2) => Division.South,
			(Division.West, 0) => Division.North,
			(Division.West, 1) => Division.South,
			(Division.West, 2) => Division.East,
			_ => throw new Exception($"No intra-conference rotation defined for division {division} and season {season}")
		};
	}

	private Division GetInterRotation(Division division, int season)
	{
		return (division, season % 4) switch
		{
			(Division.North, 0) => Division.North,
			(Division.North, 1) => Division.South,
			(Division.North, 2) => Division.East,
			(Division.North, 3) => Division.West,
			(Division.South, 0) => Division.South,
			(Division.South, 1) => Division.East,
			(Division.South, 2) => Division.West,
			(Division.South, 3) => Division.North,
			(Division.East, 0) => Division.East,
			(Division.East, 1) => Division.West,
			(Division.East, 2) => Division.North,
			(Division.East, 3) => Division.South,
			(Division.West, 0) => Division.West,
			(Division.West, 1) => Division.North,
			(Division.West, 2) => Division.South,
			(Division.West, 3) => Division.East,
			_ => throw new Exception($"No inter-conference rotation defined for division {division} and season {season}")
		};
	}

	// ---------------------------------------------------------------------------
	// Inner types
	// ---------------------------------------------------------------------------

	private class Matchup
	{
		public int OpponentID { get; set; }
		public bool IsHome { get; set; }
	}
}