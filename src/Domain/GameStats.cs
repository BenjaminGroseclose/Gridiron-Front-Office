using System.ComponentModel.DataAnnotations.Schema;

namespace GridironFrontOffice.Domain;

public class GameStats : BaseEntity
{
	public int GameStatsID { get; set; }

	public int GameID { get; set; }
	public Game Game { get; set; }

	public int TeamID { get; set; }
	public Team Team { get; set; }

	public int PassingYards { get; set; }
	public int RushingYards { get; set; }
	[NotMapped]
	public int TotalYards { get { return PassingYards + RushingYards; } }

	public int PassingTouchdowns { get; set; }
	public int RushingTouchdowns { get; set; }
	[NotMapped]
	public int TotalTouchdowns { get { return PassingTouchdowns + RushingTouchdowns; } }

	public int Interceptions { get; set; }
	public int FumblesRecovered { get; set; }
	[NotMapped]
	public int Turnovers { get { return Interceptions + FumblesRecovered; } }

	public int Sacks { get; set; }
	public int Tackles { get; set; }
	public int FieldGoalsMade { get; set; }
	public int FieldGoalsAttempted { get; set; }
	public override int ID => GameStatsID;
}