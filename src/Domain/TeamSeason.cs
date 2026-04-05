namespace GridironFrontOffice.Domain;

public class TeamSeason : BaseEntity
{
	/// <summary>
	/// The unique identifier for the TeamSeason.
	/// </summary>
	public int TeamSeasonID { get; set; }

	/// <summary>
	/// The ID of the team.
	/// </summary>
	public int TeamID { get; set; }

	public Team Team { get; set; }

	/// <summary>
	/// The ID of the season.
	/// </summary>
	public int SeasonID { get; set; }

	/// <summary>
	/// The number of wins the team has in the season.
	/// </summary>
	public int Wins { get; set; }

	/// <summary>
	/// The number of losses the team has in the season.
	/// </summary>
	public int Losses { get; set; }

	/// <summary>
	/// The number of ties the team has in the season.
	/// </summary>
	public int Ties { get; set; }

	public override int ID
	{
		get => TeamSeasonID;
	}
}