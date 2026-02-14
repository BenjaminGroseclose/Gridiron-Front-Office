namespace GridironFrontOffice.Domain.Forms;

public class LeagueSetupForm
{
	// Coach Profile
	public string? CoachName { get; set; }

	/// <summary>
	/// The coach's date of birth, used to calculate their age and determine if they are eligible to coach
	/// Rookie, Intermediate, and Veteran
	/// </summary>
	public string? CoachExperience { get; set; }

	/// <summary>
	/// The coach's coaching philosophy, which will influence their preferences for player attributes and team strategy
	/// Offensive, Defensive, Balanced
	/// </summary>
	public string? CoachPhilosophy { get; set; }

	// League Settings
	public string? LeagueName { get; set; }
	public int? RosterSize { get; set; }
	public int? PracticeSquadSize { get; set; }

	public bool? InjuriesEnabled { get; set; }
	public bool? CanBeFired { get; set; }

	/// <summary>
	/// Whether injuries are enabled in the league, which will cause players to miss games and require management of injured reserve
	/// 200M, 255M, 300M. In millions of dollars, and should be a multiple of 5 million to simplify player salary calculations
	/// </summary>
	public double? SalaryCap { get; set; }

	/// <summary>
	/// The salary cap floor for the league, which will require teams to spend a minimum amount on player salaries and prevent them from fielding extremely low-cost teams
	/// 90%, 95%, 100%
	/// </summary>
	public double? SalaryCapFloor { get; set; }

	public int? StartingYear { get; set; }

	public bool IsValid()
	{
		return !string.IsNullOrEmpty(CoachName) &&
			   !string.IsNullOrEmpty(CoachExperience) &&
			   !string.IsNullOrEmpty(LeagueName) &&
			   RosterSize.HasValue &&
			   PracticeSquadSize.HasValue &&
			   InjuriesEnabled.HasValue &&
			   CanBeFired.HasValue &&
			   SalaryCap.HasValue &&
			   SalaryCapFloor.HasValue &&
			   StartingYear.HasValue;
	}
}