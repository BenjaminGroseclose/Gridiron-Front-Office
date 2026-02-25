namespace GridironFrontOffice.Persistence.Models;

/// <summary>
/// Player archetype class represents a specific type of player with defined attributes and characteristics.
/// This class is used to categorize players into different archetypes based on their playing style, physical attributes, and skill sets. 
/// Each archetype has a name, description, associated position, selection weight for player generation.
/// 
/// Also includes the height, weight and attribute standard deviation. Where the first number in the array represents the mean value for that attribute,
/// and the second number represents the standard deviation.
/// This allows for more realistic player generation by creating players that have attributes that vary around a mean
/// </summary>
public class PlayerArchetype
{
	public string Name { get; set; }

	/// <summary>
	/// The weight that determines how likely this archetype is to be selected when generating a player for the associated position.
	/// </summary>
	public int Weight { get; set; }

	/// <summary>
	/// A dictionary where the key is the name of the attribute (e.g., "Speed", "Strength", "Agility") and the value is an array of two integers.
	/// The first integer in the array represents the mean value for that attribute, and the second integer represents the standard deviation. 
	/// This allows for more realistic player generation by creating players that have attributes that vary around a mean.
	/// </summary>
	public Dictionary<string, int[]> AttributeStandardDeviation { get; set; }
}