using System.ComponentModel.DataAnnotations;

namespace GridironFrontOffice.Domain;

public class PlayerStats : BaseEntity
{
	[Key]
	public int PlayerStatsID { get; set; }

	public int PlayerID { get; set; }
	public Player Player { get; set; }

	public int Season { get; set; }

	public int GameID { get; set; }
	public Game Game { get; set; }

}