using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Persistence.Models;

namespace GridironFrontOffice.Application.Interfaces;

public interface IPlayerGeneratorService
{
	/// <summary>
	/// Generates a player for a specific position. The implementation of this method can use various algorithms or data sources to create a player with attributes that are suitable for the given position.
	/// </summary>
	/// <param name="position">The position for which the player is to be generated.</param>
	/// <param name="namePool">An optional pool of names to use for generating player names. If not provided, the implementation can use a default set of names.</param>
	/// <param name="archetypesByPosition">An optional dictionary mapping player positions to
	/// lists of player archetypes. This can be used to bias the generation of player attributes based on their position and archetype. If not provided, the implementation can use a default set of archetypes.</param>
	/// <param name="age">An optional age for the player. If not provided, the player is assumed to a rookie.</param>
	/// <returns>The generated player for the specified position.</returns>
	Task<Player> GeneratePlayerAsync(PlayerPosition position, NamePool namePool = null, Dictionary<PlayerPosition, List<PlayerArchetype>> archetypesByPosition = null, int age = 0);

	/// <summary>
	/// Generates a list of players for a team. The number of players generated and their positions can be determined by the implementation of this method.
	/// Should generate a balanced team with players in various positions, ensuring that the team has the necessary roles filled to compete effectively.
	/// the balance of the team may vary based on randomness (some team may hav 3 runnings back while other might have 4)
	/// </summary>
	/// <returns>The list of generated players for the team.</returns>
	Task<IEnumerable<Player>> GeneratePlayersForTeamAsync(int teamID, int startYear);
}