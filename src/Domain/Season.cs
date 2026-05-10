using System.ComponentModel.DataAnnotations.Schema;
using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class Season : BaseEntity
{
	/// <summary>
	/// The year the season takes place in.
	/// </summary>
	[NotMapped]
	public int Year => ID;

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

	public DateOnly RegularSeasonStartDate { get; set; }

	/// <summary>
	/// The end date of the season.
	/// </summary>
	public DateOnly EndDate { get; set; }

}