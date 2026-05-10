namespace GridironFrontOffice.Domain;

public class Game : BaseEntity
{
	public int SeasonID { get; set; }

	public int WeekID { get; set; }

	public Week Week { get; set; }

	public int HomeTeamID { get; set; }
	public Team HomeTeam { get; set; }

	public int AwayTeamID { get; set; }
	public Team AwayTeam { get; set; }

	public int? HomeTeamScore { get; set; }
	public int? AwayTeamScore { get; set; }

	public DateTime GameDateTime { get; set; }

	// Computed
	public bool IsCompleted => HomeTeamScore.HasValue && AwayTeamScore.HasValue;
	public int? WinningTeamID => IsCompleted ? (HomeTeamScore > AwayTeamScore ? HomeTeamID : AwayTeamID) : null;
	public int? PointDifferential => IsCompleted ? Math.Abs(HomeTeamScore.Value - AwayTeamScore.Value) : null;
	public Team WinningTeam => IsCompleted ? (HomeTeamScore > AwayTeamScore ? HomeTeam : AwayTeam) : null;
}