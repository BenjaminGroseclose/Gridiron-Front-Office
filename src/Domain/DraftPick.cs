using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class DraftPick : BaseEntity
{
	public int DraftPickID { get; set; }

	/// <summary>
	/// The team ID of the team that owns this draft pick.
	///  This is a foreign key that links to the Team entity, 
	/// allowing us to identify which team has the rights to this draft pick.
	/// </summary>
	public int TeamID { get; set; }
	public Team Team { get; set; }

	/// <summary>
	/// The season in which this draft pick will be used. This is important for tracking the draft 
	/// picks across multiple seasons, especially if they are traded. 
	/// This should correspond to the season of the draft for which this pick is relevant.
	/// </summary>
	public int SeasonID { get; set; }

	/// <summary>
	/// The round in which this draft pick will be used. Round range from 1 - 7. 
	/// </summary>
	public int Round { get; set; }

	/// <summary>
	/// The overall pick number of this draft pick in the draft. This is a unique identifier 
	/// for the pick within the draft and is used to determine the order of selection.
	/// 
	/// This is calculated based at the start of Post Season and after each round of the playoffs.
	/// The worst record team gets the first pick in each round, and the Super Bowl winner gets the last pick in each round.
	/// 
	/// For example, in a 32 team league, the first round picks would be 1-32, the second round picks would be 33-64, and so on.
	/// </summary>
	public int? OverallPickNumber { get; set; }

	/// <summary>
	/// The type of draft pick, which can be used to differentiate between regular draft picks, compensatory picks, and expansion picks.
	/// </summary>
	public DraftPickType DraftPickType { get; set; }

	public override int ID => this.DraftPickID;
}