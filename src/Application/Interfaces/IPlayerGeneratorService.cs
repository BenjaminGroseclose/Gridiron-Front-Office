using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Persistence;
using GridironFrontOffice.Persistence.Models;

namespace GridironFrontOffice.Application.Interfaces;

public interface IPlayerGeneratorService
{
	/// <summary>
	/// Generates a player for a specific position. The implementation of this method can use various algorithms or data sources to create a player with attributes that are suitable for the given position.
	/// </summary>
	/// <param name="position">The position for which the player is to be generated.</param>
	/// <returns>The generated player for the specified position.</returns>
	Task<Player> GeneratePlayerAsync(PlayerPosition position, NamePool namePool = null);

	/// <summary>
	/// Generates a list of players for a team. The number of players generated and their positions can be determined by the implementation of this method.
	/// Should generate a balanced team with players in various positions, ensuring that the team has the necessary roles filled to compete effectively.
	/// the balance of the team may vary based on randomness (some team may hav 3 runnings back while other might have 4)
	/// </summary>
	/// <returns>The list of generated players for the team.</returns>
	Task<IEnumerable<Player>> GeneratePlayersForTeamAsync(int teamID);
}