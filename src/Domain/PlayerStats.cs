namespace GridironFrontOffice.Domain;

public class PlayerStats : BaseEntity
{
	public int PlayerStatsID { get; set; }

	public int PlayerID { get; set; }
	public Player Player { get; set; }

	public int SeasonID { get; set; }

	public int GameID { get; set; }
	public Game Game { get; set; }

}