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
	private readonly int[] BYE_WEEKS = [5, 6, 7, 8, 9, 10, 11, 12];

	private readonly IBaseRepository<Game> _gameRepository;
	private readonly IBaseRepository<Season> _seasonRepository;
	private readonly IBaseRepository<Week> _weekRepository;
	private readonly ITeamService _teamService;
	private readonly ILogger<ScheduleService> _logger;

	public ScheduleService(IBaseRepository<Game> gameRepository,
						   ITeamService teamService,
						   IBaseRepository<Season> seasonRepository,
						   IBaseRepository<Week> weekRepository,
						   ILogger<ScheduleService> logger)
	{
		_gameRepository = gameRepository;
		_teamService = teamService;
		_seasonRepository = seasonRepository;
		_weekRepository = weekRepository;
		_logger = logger;
	}

	public async Task<Season> GetCurrentSeason()
	{
		var seasons = await _seasonRepository.GetAllAsync();

		var currentSeason = seasons.FirstOrDefault(s => s.IsCurrentSeason);

		if (currentSeason == null)
		{
			throw new DomainException("No current season set, data may be corrupted", "NO_CURRENT_SEASON");
		}

		return currentSeason;
	}

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
			_logger.LogInformation("No previous season found for seasonID {PreviousSeasonID}. This may be the first season.", seasonID - 1);
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

		// Generate every team's matchup list, deduplicate into unique pairs,
		// then assign home/away to balance each team's home game count.
		var pairCount = new Dictionary<(int, int), int>();
		var uniquePairs = new List<(int TeamA, int TeamB, bool IsDivisional)>();
		int totalMatchups = 0;

		foreach (var team in teams)
		{
			var matchups = GenerateOpponents(team, teams, seasonID, divisionalRankings, divisionTeamsByRank);

			_logger.LogInformation("Generated {NumMatchups} matchups for team {TeamID} in season {SeasonID}", matchups.Count, team.TeamID, seasonID);
			totalMatchups += matchups.Count;

			foreach (var matchup in matchups)
			{
				// Normalize key so {A,B} and {B,A} share the same counter
				var ids = matchup.TeamsInvolved.OrderBy(id => id).ToArray();
				var key = (ids[0], ids[1]);
				int count = pairCount.GetValueOrDefault(key, 0);
				int maxGames = matchup.Type == MatchupType.Divisional ? 2 : 1;

				if (count >= maxGames)
				{
					continue;
				}

				pairCount[key] = count + 1;
				uniquePairs.Add((key.Item1, key.Item2, matchup.Type == MatchupType.Divisional));
			}
		}

		// Assign home/away to balance each team's home game count
		var homeCount = new Dictionary<int, int>();
		foreach (var team in teams)
		{
			homeCount[team.TeamID] = 0;
		}

		var rawGames = new List<Game>();
		var rng = new Random(seasonID);

		// Shuffle pairs so the balancing doesn't always favor the same teams
		var shuffledPairs = uniquePairs.OrderBy(_ => rng.Next()).ToList();

		foreach (var (teamA, teamB, isDivisional) in shuffledPairs)
		{
			if (isDivisional)
			{
				// Divisional: one game home, one away — each team hosts once
				rawGames.Add(new Game { SeasonID = seasonID, HomeTeamID = teamA, AwayTeamID = teamB });
				rawGames.Add(new Game { SeasonID = seasonID, HomeTeamID = teamB, AwayTeamID = teamA });
				homeCount[teamA]++;
				homeCount[teamB]++;
			}
			else
			{
				// Non-divisional: assign home to the team with fewer home games so far
				int homeID, awayID;
				if (homeCount[teamA] <= homeCount[teamB])
				{
					homeID = teamA;
					awayID = teamB;
				}
				else
				{
					homeID = teamB;
					awayID = teamA;
				}

				rawGames.Add(new Game { SeasonID = seasonID, HomeTeamID = homeID, AwayTeamID = awayID });
				homeCount[homeID]++;
			}
		}

		_logger.LogInformation("Generated a total of {TotalMatchups} matchups for season {SeasonID}, resulting in {GameCount} unique games after deduplication.", totalMatchups, seasonID, rawGames.Count);

		// Assign games to weeks and set dates
		var games = AssignGamesToWeeks(rawGames, teams, weeks, currentSeason);

		_logger.LogInformation("Created {GameCount} games for season {SeasonID}", games.Count, seasonID);

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

		// 1. Divisional matchups — 3 opponents, each played twice (home + away) = 6 games
		var divisionalOpponents = allTeams
			.Where(t => t.Division == team.Division && t.Conference == team.Conference && t.TeamID != team.TeamID)
			.ToList();

		foreach (var opponent in divisionalOpponents)
		{
			matchups.Add(new Matchup { TeamsInvolved = new HashSet<int> { team.TeamID, opponent.TeamID }, Type = MatchupType.Divisional });
		}

		_logger.LogInformation("Matchup Count: Added {NumDivisional} divisional matchups for team {TeamID} in season {SeasonID}", divisionalOpponents.Count, team.TeamID, season);

		// 2. Intra-conference rotation — all 4 teams from one same-conf division = 4 games
		Division intraRotation = GetIntraRotation(team.Division, season);
		var intraConferenceOpponents = allTeams
			.Where(t => t.Division == intraRotation && t.Conference == team.Conference)
			.ToList();

		foreach (var opponent in intraConferenceOpponents)
		{
			matchups.Add(new Matchup { TeamsInvolved = new HashSet<int> { team.TeamID, opponent.TeamID }, Type = MatchupType.IntraConference });
		}

		_logger.LogInformation("Matchup Count: Added {NumIntraConference} intra-conference matchups for team {TeamID} in season {SeasonID}", intraConferenceOpponents.Count, team.TeamID, season);

		// 3. Inter-conference rotation — all 4 teams from one opposite-conf division = 4 games
		Division interRotation = GetInterRotation(team.Division, season);
		var interConferenceOpponents = allTeams
			.Where(t => t.Division == interRotation && t.Conference != team.Conference)
			.ToList();

		foreach (var opponent in interConferenceOpponents)
		{
			matchups.Add(new Matchup { TeamsInvolved = new HashSet<int> { team.TeamID, opponent.TeamID }, Type = MatchupType.InterConference });
		}

		_logger.LogInformation("Matchup Count: Added {NumInterConference} inter-conference matchups for team {TeamID} in season {SeasonID}", interConferenceOpponents.Count, team.TeamID, season);

		// 4. Same-rank intra-conference — 1 game each from the 2 remaining same-conf divisions = 2 games
		var allDivisions = new[] { Division.North, Division.South, Division.East, Division.West };
		var remainingIntraDivisions = allDivisions
			.Where(d => d != team.Division && d != intraRotation)
			.ToList();

		for (int i = 0; i < remainingIntraDivisions.Count; i++)
		{
			var key = (team.Conference, remainingIntraDivisions[i]);
			if (!divisionTeamsByRank.TryGetValue(key, out var rankList))
				continue;

			// Find opponent at same rank; clamp in case division has fewer teams
			int rankIndex = Math.Min(myRank - 1, rankList.Count - 1);
			var opponent = rankList[rankIndex];

			matchups.Add(new Matchup { TeamsInvolved = new HashSet<int> { team.TeamID, opponent.TeamID }, Type = MatchupType.IntraConference });
		}

		_logger.LogInformation("Matchup Count: Added {NumSameRankIntraConference} same-rank intra-conference matchups for team {TeamID} in season {SeasonID}", remainingIntraDivisions.Count, team.TeamID, season);

		// 5. 17th game — same rank, opposite conference, using explicit symmetric rotation
		Division seventeenthDiv = GetSeventeenthGameRotation(team.Division, season);
		var oppositeConference = team.Conference == Conference.AFC ? Conference.NFC : Conference.AFC;
		var seventeenthKey = (oppositeConference, seventeenthDiv);
		if (divisionTeamsByRank.TryGetValue(seventeenthKey, out var seventeenthRankList))
		{
			int rankIndex = Math.Min(myRank - 1, seventeenthRankList.Count - 1);
			var seventeenth = seventeenthRankList[rankIndex];

			matchups.Add(new Matchup { TeamsInvolved = new HashSet<int> { team.TeamID, seventeenth.TeamID }, Type = MatchupType.SameRankInterConference });
		}

		_logger.LogInformation("Matchup Count: Added 17th game (same-rank, opposite conference) for team {TeamID} in season {SeasonID}", team.TeamID, season);

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

		_logger.LogInformation("Assigning total number of games {NumGames} to weeks for season {SeasonID}. Distributing byes for {NumTeams} teams across bye weeks {ByeWeeks}.", games.Count, season.SeasonID, teams.Count, string.Join(", ", BYE_WEEKS));

		for (int i = 0; i < teamsInByeOrder.Count; i++)
		{
			int week = BYE_WEEKS[i % BYE_WEEKS.Length];
			byeWeek[teamsInByeOrder[i].TeamID] = week;
		}

		// --- Shuffle for variety (seeded for determinism per season) ---
		var rng = new Random(season.SeasonID);
		var shuffled = games.OrderBy(_ => rng.Next()).ToList();
		var seasonStartDate = season.RegularSeasonStartDate;
		var assignedGames = new List<Game>();

		_logger.LogInformation("Assigning {NumGames} games to weeks for season {SeasonID} starting on {SeasonStartDate:d}", shuffled.Count, season.SeasonID, seasonStartDate);

		// Logic: Go week by week and assign game that fits that week.
		foreach (var week in weeks)
		{
			_logger.LogInformation("Assigning games for week {WeekID} of season {SeasonID}", week.WeekID, season.SeasonID);
			var teamsAssignedThisWeek = new HashSet<int>();
			var assignedGamesThisWeek = new List<Game>();

			// TODO: Add logic to prevent certain division matchups from happening within 3 weeks of each other. 
			foreach (var game in shuffled)
			{
				if (teamsAssignedThisWeek.Contains(game.HomeTeamID) || teamsAssignedThisWeek.Contains(game.AwayTeamID))
				{
					continue; // This game can't be scheduled this week since one of the teams is already playing
				}

				bool homeOnBye = byeWeek.TryGetValue(game.HomeTeamID, out int homeBye) && homeBye == week.WeekID;
				bool awayOnBye = byeWeek.TryGetValue(game.AwayTeamID, out int awayBye) && awayBye == week.WeekID;

				if (homeOnBye || awayOnBye)
				{
					continue; // Can't schedule this game this week since one of the teams is on bye
				}

				// Assign this game to the current week
				game.WeekID = week.WeekID;

				// TODO: Vary game times and days for more realism
				// Schedule games on Sundays at 1:00 PM
				var gameDateTime = seasonStartDate.AddDays((week.WeekID - 1) * 7).ToDateTime(new TimeOnly(13, 0));

				game.GameDateTime = gameDateTime;
				teamsAssignedThisWeek.Add(game.HomeTeamID);
				teamsAssignedThisWeek.Add(game.AwayTeamID);
				assignedGamesThisWeek.Add(game);
				assignedGames.Add(game);
			}
			_logger.LogInformation("Assigned {NumGames} games to week {WeekID} of season {SeasonID}", assignedGamesThisWeek.Count, week.WeekID, season.SeasonID);

			shuffled = shuffled.Except(assignedGamesThisWeek).ToList(); // Remove assigned games from the pool

			_logger.LogInformation("Completed week {WeekID} assignments for season {SeasonID}", week.WeekID, season.SeasonID);
		}

		var unassignedGames = shuffled.Count;
		if (unassignedGames > 0)
		{
			_logger.LogWarning("{UnassignedGames} games could not be assigned to weeks for season {SeasonID}. This indicates a problem with the scheduling algorithm.", unassignedGames, season.SeasonID);
		}
		else
		{
			_logger.LogInformation("All games successfully assigned to weeks for season {SeasonID}", season.SeasonID);
		}

		return assignedGames;
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

	/// <summary>
	/// Returns the opposite-conference division for the 17th game.
	/// The mapping is always symmetric (if North→South then South→North)
	/// and never overlaps with the inter-conference rotation.
	/// </summary>
	private Division GetSeventeenthGameRotation(Division division, int season)
	{
		return (division, season % 4) switch
		{
			// s%4=0: N↔S, E↔W  (inter is identity — no overlap)
			(Division.North, 0) => Division.South,
			(Division.South, 0) => Division.North,
			(Division.East, 0) => Division.West,
			(Division.West, 0) => Division.East,
			// s%4=1: N↔E, S↔W  (inter is N→S,S→E,E→W,W→N — no overlap)
			(Division.North, 1) => Division.East,
			(Division.South, 1) => Division.West,
			(Division.East, 1) => Division.North,
			(Division.West, 1) => Division.South,
			// s%4=2: N↔S, E↔W  (inter is N→E,S→W,E→N,W→S — no overlap)
			(Division.North, 2) => Division.South,
			(Division.South, 2) => Division.North,
			(Division.East, 2) => Division.West,
			(Division.West, 2) => Division.East,
			// s%4=3: N↔E, S↔W  (inter is N→W,S→N,E→S,W→E — no overlap)
			(Division.North, 3) => Division.East,
			(Division.South, 3) => Division.West,
			(Division.East, 3) => Division.North,
			(Division.West, 3) => Division.South,
			_ => throw new Exception($"No 17th-game rotation defined for division {division} and season {season}")
		};
	}

	// ---------------------------------------------------------------------------
	// Inner types
	// ---------------------------------------------------------------------------

	private enum MatchupType
	{
		Divisional,
		IntraConference,
		InterConference,
		SameRankIntraConference,
		SameRankInterConference
	}

	private class Matchup
	{
		public HashSet<int> TeamsInvolved { get; set; } = new HashSet<int>();
		public MatchupType Type { get; set; }
	}
}