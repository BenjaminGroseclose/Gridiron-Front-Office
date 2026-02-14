using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Persistence.Models;

namespace GridironFrontOffice.Persistence.Interfaces;

/// <summary>
/// Service responsible for loading default seed data from JSON resources
/// </summary>
public interface ISeedDataService
{
	/// <summary>
	/// Loads the default data from the JSON resource file
	/// </summary>
	/// <returns>A tuple containing Teams, Stadiums, Conferences, and Divisions</returns>
	Task<(IEnumerable<Team> Teams, IEnumerable<Stadium> Stadiums, IEnumerable<Conference> Conferences, IEnumerable<Division> Divisions)> LoadDefaultDataAsync();

	/// <summary>
	/// Loads the name pool data from the JSON resource file. This method is used to populate the 
	/// name pool with first names and last names that can be used for generating player names.
	/// 
	/// Results are cached in memory after the first load to improve performance on subsequent calls.
	/// </summary>
	/// <returns>A NamePool object containing first names, last names, and colleges</returns>
	Task<NamePool> LoadNamePoolAsync();

	/// <summary>
	/// Loads the player archetypes from the JSON resource file. Player archetypes define 
	/// different types of players with specific attributes and characteristics. This data can be 
	/// used to generate players with varying traits and abilities based on their archetype.
	/// 
	/// Results are cached in memory after the first load to improve performance on subsequent calls.
	/// </summary>
	Task<Dictionary<PlayerPosition, List<PlayerArchetype>>> LoadPlayerArchetypesAsync();
}
