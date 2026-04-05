using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.Application;

public class TeamService : ITeamService
{
	private readonly IBaseRepository<Team> _teamRepository;
	private readonly IBaseRepository<TeamSeason> _teamSeasonRepository;

	public TeamService(IBaseRepository<Team> teamRepository, IBaseRepository<TeamSeason> teamSeasonRepository)
	{
		_teamRepository = teamRepository;
		_teamSeasonRepository = teamSeasonRepository;
	}

	public async Task<IEnumerable<Team>> GetAllTeamsAsync() => await _teamRepository.GetAllAsync();

	public async Task<Team> GetTeam(int teamID)
	{
		var team = await _teamRepository.GetByIDAsync(teamID);

		if (team == null)
		{
			throw new Exception($"Team with ID {teamID} not found.");
		}

		return team;
	}

	public async Task<List<Standing>> GetTeamStandings(int seasonID)
	{
		var teamSeason = await _teamSeasonRepository.GetAllAsync();

		return this.CalculateStandings(teamSeason.Where(ts => ts.SeasonID == seasonID));
	}
	public async Task<List<Standing>> GetTeamStandings(int seasonID, Conference conference)
	{
		var teamSeason = await _teamSeasonRepository.GetAllAsync();

		return this.CalculateStandings(teamSeason.Where(ts => ts.SeasonID == seasonID && ts.Team.Conference == conference));

	}

	public async Task<List<Standing>> GetTeamStandings(int seasonID, Conference conference, Division division)
	{
		var teamSeason = await _teamSeasonRepository.GetAllAsync();

		return this.CalculateStandings(teamSeason.Where(ts => ts.SeasonID == seasonID && ts.Team.Conference == conference && ts.Team.Division == division));
	}

	private List<Standing> CalculateStandings(IEnumerable<TeamSeason> teamSeasons)
	{
		var standings = teamSeasons.Select(ts => new Standing
		{
			Team = ts.Team,
			Wins = ts.Wins,
			Losses = ts.Losses,
			Ties = ts.Ties
		})
		.OrderByDescending(s => s.Wins)
		.ThenBy(s => s.Losses)
		.ThenBy(s => s.Ties)
		.ToList();

		for (int i = 0; i < standings.Count; i++)
		{
			standings[i].Ranking = i + 1;
		}

		return standings;
	}
}