using GridironFrontOffice.Domain.Enums;
using SQLite;

namespace GridironFrontOffice.Domain;

public class League : BaseEntity
{
	[PrimaryKey, AutoIncrement]
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
	public DateOnly CurrentDate { get; set; }

	// Player Information

	/// <summary>
	/// The name of the player managing the team.
	/// </summary>
	public string PlayerName { get; set; }

	/// <summary>
	/// The date of birth of the player managing the team.
	/// </summary>
	public DateOnly PlayerDateOfBirth { get; set; }

	// League Settings

	/// <summary>
	/// The league difficulty setting for the league.
	/// The AI will be easier or harder to compete against based on this setting.
	/// For example, a harder difficulty might have the AI make better trades, manage their roster more effectively, and perform better in games.
	/// </summary>
	public LeagueDifficultyLevel LeagueDifficulty { get; set; }

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