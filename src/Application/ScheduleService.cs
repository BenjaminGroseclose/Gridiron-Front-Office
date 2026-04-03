using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.Application;

public class ScheduleService : IScheduleService
{
	private readonly IBaseRepository<Game> _gameRepository;
	private readonly IBaseRepository<Season> _seasonRepository;

	public ScheduleService(IBaseRepository<Game> gameRepository, IBaseRepository<Season> seasonRepository)
	{
		_gameRepository = gameRepository;
		_seasonRepository = seasonRepository;
	}

	public Task<bool> CreateFreshSchedule(int seasonID) => throw new NotImplementedException();
	public Task<bool> CreateScheduleFromPreviousSeason(int seasonID, int previousSeasonID) => throw new NotImplementedException();
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