using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class League : BaseEntity
{
	public int LeagueID { get; set; }

	/// <summary>
	/// The name of the league. Should match the filename of the database.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The current season year for the league.
	/// </summary>
	public int CurrentSeason { get; set; }

	/// <summary>
	/// The current date in the league.
	/// </summary>
	public DateTime CurrentDate { get; set; }

	/// <summary>
	/// The data source for the league. Determines how the league data is stored and loaded.
	/// <see cref="LeagueDataSource"/>
	/// </summary>
	public LeagueDataSource DataSource { get; set; }

	// League Settings

	/// <summary>
	/// Whether injuries are enabled in the league.
	/// </summary>
	public bool InjuriesEnabled { get; set; }

	public int NumOfPlayoffsTeams { get; set; }

	public int NumOfRegularSeasonGames { get; set; }

	/// <summary>
	/// The roster size for teams in the league.
	/// </summary>
	public int RosterSize { get; set; }

	/// <summary>
	/// The practice squad size for teams in the league.
	/// </summary>
	public int PracticeSquadSize { get; set; }

	/// <summary>
	/// Whether the player can be fired during the season.
	/// </summary>
	public bool CanBeFired { get; set; }

	public override int ID
	{
		get => LeagueID;
	}
}