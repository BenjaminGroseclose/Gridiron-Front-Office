using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;

namespace GridironFrontOffice.Application;

public class PlayerService : IPlayerService
{
	private readonly IBaseRepository<Player> _playerRepository;

	public PlayerService(IBaseRepository<Player> playerRepository)
	{
		this._playerRepository = playerRepository;
	}

	public async Task<Player> GetPlayerByID(int playerID, bool includeContracts = false)
	{
		var player = await _playerRepository.GetByIDAsync(playerID, includes: includeContracts ? p => p.Contracts : null);

		if (player == null)
		{
			throw new DomainException($"Player with ID {playerID} not found.");
		}

		return player;
	}

	public async Task<IEnumerable<Player>> GetPlayersForTeam(int teamID)
	{
		var allPlayers = await _playerRepository.GetAllAsync(
			includes: p => p.Contracts
		);
		return allPlayers.Where(p => p.CurrentTeam != null && p.CurrentTeam.ID == teamID).ToList();
	}

	public async Task<IEnumerable<Player>> GetPlayersForTeam(int seasonID, int teamID)
	{
		var allPlayers = await _playerRepository.GetAllAsync(
			includes: p => p.Contracts
		);
		return allPlayers.Where(p =>
			p.Contracts.Any(c => c.StartYear <= seasonID && c.EndYear >= seasonID && c.YearlyBreakdown.Any(y => y.TeamID == teamID && y.SeasonID == seasonID))
		).ToList();
	}
}