using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class Week : BaseEntity
{
	public int SeasonID { get; set; }

	/// <summary>
	/// The name of the week. eg "Week 1", "Preseason Week 2", "Playoffs Round 1"
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The number of the week in the season.
	/// </summary>
	public int WeekNumber { get; set; }

	public IEnumerable<Game> Games { get; set; }

	/// <summary>
	/// The type of the week (PreSeason, RegularSeason, Playoffs, SuperBowl).
	/// </summary>
	public WeekType Type { get; set; }

}