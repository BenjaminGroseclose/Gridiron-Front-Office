using GridironFrontOffice.Domain.Enums;

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

	public string Description { get; set; }

	/// <summary>
	/// The position associated with this player archetype. This indicates the role or 
	/// position that players of this archetype are likely to play on the field.
	/// </summary>
	public PlayerPosition Position { get; set; }

	/// <summary>
	/// A weight used to determine how likely this archetype is to be 
	/// selected when generating players. Higher values indicate a greater likelihood of selection.
	/// </summary>
	public int SelectionWeight { get; set; }

	public int[] HeightRange { get; set; }

	public int[] WeightRange { get; set; }

	public Dictionary<string, int[]> AttributeStandardDeviation { get; set; }
}