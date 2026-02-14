namespace GridironFrontOffice.Persistence.Models;



public class College
{
	public string Name { get; set; } = string.Empty;
	public int Tier { get; set; } = 0;

	/// <summary>
	/// The weight of the college, which can be used to determine the likelihood of a player being generated from this college. 
	/// A higher weight means that players from this college are more likely to be generated, while a lower weight 
	/// means that players from this college are less likely to be generated. The weight can be used in the player 
	/// generation algorithm to create a more realistic distribution of players from different colleges.
	/// </summary>
	public int Weight { get; set; }

	public string Conference { get; set; } = string.Empty;
}