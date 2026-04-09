namespace GridironFrontOffice.Domain;

public class Game : BaseEntity
{
	public int GameID { get; set; }

	public int SeasonID { get; set; }

	public int WeekID { get; set; }

	public Week Week { get; set; }

	public int HomeTeamID { get; set; }
	public int AwayTeamID { get; set; }

	public int? HomeTeamScore { get; set; }
	public int? AwayTeamScore { get; set; }

	public DateTime GameDateTime { get; set; }

	public override int ID
	{
		get => GameID;
	}

}