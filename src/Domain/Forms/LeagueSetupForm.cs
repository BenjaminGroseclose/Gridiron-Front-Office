using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain.Forms;

public class LeagueSetupForm
{
	// Profile Settings
	public string LeagueName { get; set; } = string.Empty;
	public string CoachName { get; set; } = string.Empty;
	public DateTime CoachDateOfBirth { get; set; }
	public LeagueDifficultyLevel LeagueDifficulty { get; set; }

	// Team Selection
	public int SelectedTeamID { get; set; }

	// League Settings
	public bool InjuriesEnabled { get; set; }
	public int NumOfPlayoffsTeams { get; set; }
	public int NumOfRegularSeasonGames { get; set; }
	public int RosterSize { get; set; }
	public int PracticeSquadSize { get; set; }
	public bool CanBeFired { get; set; }
}