using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class Season : BaseEntity
{
	/// <summary>
	/// The unique identifier for the season.
	/// This should be the year of the season (e.g. 2024 for the 2024 season).
	/// </summary>
	public int SeasonID { get; set; }

	/// <summary>
	/// The year the season takes place in.
	/// </summary>
	public int Year => SeasonID;

	/// <summary>
	/// The status of the season.
	/// </summary>
	public SeasonStatus Status { get; set; }

	/// <summary>
	/// Indicates if this is the current active season.
	/// </summary>
	public bool IsCurrentSeason { get; set; }

	/// <summary>
	/// The start date of the season.
	/// </summary>
	public DateOnly StartDate { get; set; }

	/// <summary>
	/// The end date of the season.
	/// </summary>
	public DateOnly EndDate { get; set; }

	public override int ID
	{
		get => SeasonID;
	}
}