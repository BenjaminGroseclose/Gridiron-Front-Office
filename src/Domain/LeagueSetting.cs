using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

public class LeagueSetting : BaseEntity
{
	public int LeagueSettingID { get; set; }

	/// <summary>
	/// The name of the league. Should match the filename of the database.
	/// This also should change year over year
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The season that these settings apply to. This should change year over year.
	/// </summary>
	public int SeasonID { get; set; }

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

	/// <summary>
	/// The number of regular season weeks in the league. This should be set before creating the schedule for the season.
	/// </summary>
	public int NumOfRegularSeasonWeeks { get; set; }

	/// <summary>
	/// The number of bye weeks in the league. This should be set before creating the schedule for the season.
	/// </summary>
	public int NumOfByeWeeks { get; set; }

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

	/// <summary>
	/// The salary cap for teams in the league.
	/// </summary>
	public double SalaryCap { get; set; }

	public double SalaryFloor { get { return SalaryCap * 0.75; } }

	public override int ID
	{
		get => LeagueSettingID;
	}
}