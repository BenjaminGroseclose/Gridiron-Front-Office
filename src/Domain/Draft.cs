namespace GridironFrontOffice.Domain;

/// <summary>
/// Represents a draft in the league. 
/// A draft is associated with a specific season and 
/// contains information about the draft picks for that season.
/// 
/// Draft Picks are represented by the DraftPick entity, which includes details 
/// about the team that owns the pick, the round of the pick, and the season in which the pick will be used.
/// </summary>
public class Draft : BaseEntity
{
	public int DraftID { get; set; }
	public int SeasonID { get; set; }
	public Season Season { get; set; }

	/// <summary>
	/// The date of the draft, this date will be used to stop the simulation, and allow the user to make their draft picks.
	/// The draft should occur after the end of the season, and before the start of the next season.
	/// </summary>
	public DateOnly DraftDate { get; set; }
	public string Location { get; set; }

	public override int ID => this.DraftID;
}