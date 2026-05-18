using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Application.Interfaces;

public interface IPlayerService
{
	/// <summary>
	/// Gets a list of players for a given team. Optionally includes contract information.
	/// This is always in reference to the current season and the player's current team.
	/// 
	/// Players contract data is always included because it is needed to determine if the player is currently on the team, 
	/// and to display contract information on the roster page.
	/// </summary>
	/// <param name="teamID">The ID of the team for which to retrieve players.</param>
	/// <returns>A list of players for the specified team.</returns>
	Task<IEnumerable<Player>> GetPlayersForTeam(int teamID);

	/// <summary>
	/// Gets a list of players for a given team in a specific season. Optionally includes contract information.
	/// This allows us to see the roster for a team in a specific season, which is important for historical 
	/// data and understanding how the team's roster has changed over time. By including the seasonID, 
	/// we can filter the players to those who were on the team during that season, which is useful for 
	/// analyzing team performance, player development, and roster decisions in a specific context.
	/// 
	/// Players contract data is always included because it is needed to determine if the player was on the team during the specified season, 
	/// and to display contract information on the roster page.
	/// </summary>
	/// <param name="seasonID">The ID of the season for which to retrieve players.</param>
	/// <param name="teamID">The ID of the team for which to retrieve players.</param>
	/// <returns>A list of players for the specified team in the specified season.</returns>
	Task<IEnumerable<Player>> GetPlayersForTeam(int seasonID, int teamID);

	/// <summary>
	/// Gets a player by their ID. Optionally includes contract information.
	/// </summary>
	/// <param name="playerID">The ID of the player to retrieve.</param>
	/// <param name="includeContracts">Whether to include contract information for the player.</param>
	/// <returns>The player with the specified ID.</returns>
	Task<Player> GetPlayerByID(int playerID, bool includeContracts = false);
}