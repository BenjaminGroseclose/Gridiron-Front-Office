using GridironFrontOffice.Domain;

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
}
