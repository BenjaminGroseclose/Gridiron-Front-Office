namespace GridironFrontOffice.Persistence.Models;

/// <summary>
/// Class representing a pool of names and colleges for player generation. 
/// This class is used to store the first names, last names, and colleges that can be 
/// used when generating players. The data for this class can be loaded from a JSON resource file, 
/// allowing for easy customization and expansion of the name pool without modifying the code. 
/// The name pool can be used in the player generation algorithm to create more realistic and 
/// varied player profiles by randomly selecting names and colleges from the provided lists.
/// </summary>
public class NamePool
{
	public IEnumerable<string> FirstNames { get; set; } = new List<string>();
	public IEnumerable<string> LastNames { get; set; } = new List<string>();
	public IEnumerable<College> Colleges { get; set; } = new List<College>();
}
