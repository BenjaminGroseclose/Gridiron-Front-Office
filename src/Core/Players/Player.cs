using GridironFrontOffice.Core.IDs;

namespace GridironFrontOffice.Core.Players;

// Core/Players/Player.cs
/// <summary>
/// A player's row (F-03 §5): bio and draft history, then one column per Measurable, PlayerAttribute and
/// PersonalityTrait, named after the member, and the hidden ratings. True values: screens see them through
/// IPerception (WP6).
/// </summary>
public sealed class Player
{
	public PlayerID PlayerID { get; set; }

	/// <summary>
	/// The team's ID, null when a free agent
	/// </summary>
	public TeamID? TeamID { get; set; }

	/// <summary>
	/// <see cref="PlayerStatus"/> 
	/// Active or FreeAgent at creation
	/// </summary>
	public PlayerStatus Status { get; set; }
	public Position Position { get; set; }
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public DateOnly BirthDate { get; set; }
	public CollegeID CollegeID { get; set; }
	public string HometownName { get; set; } = "";
	public string HometownState { get; set; } = "";
	public int Experience { get; set; }

	// the four draft columns are all null when undrafted
	public int? DraftSeason { get; set; }
	public int? DraftRound { get; set; }

	/// <summary>
	/// Their overall draft pick, 1-224
	/// </summary>
	public int? DraftPick { get; set; }
	public TeamID? DraftTeamID { get; set; }
	public int? JerseyNumber { get; set; }               // null for free agents

	// Measurable's members, in its order: inches, pounds, seconds, reps of 225 lb
	#region Measurables
	public double Height { get; set; }
	public double Weight { get; set; }
	public double Forty { get; set; }
	public double TenSplit { get; set; }
	public double ThreeCone { get; set; }
	public double Shuttle { get; set; }
	public double Bench { get; set; }
	public double Vertical { get; set; }
	public double BroadJump { get; set; }
	public double ArmLength { get; set; }
	public double HandSize { get; set; }
	#endregion

	// PlayerAttribute's 42 members, in its order (0–100, unrounded)
	#region PlayerAttribute
	// Physical
	public double Speed { get; set; }
	public double Acceleration { get; set; }
	public double Agility { get; set; }
	public double Strength { get; set; }
	public double Stamina { get; set; }
	public double Toughness { get; set; }

	// Mental
	public double Awareness { get; set; }
	public double Discipline { get; set; }

	// Passing
	public double ArmStrength { get; set; }
	public double ShortAccuracy { get; set; }
	public double MediumAccuracy { get; set; }
	public double DeepAccuracy { get; set; }
	public double ThrowOnTheRun { get; set; }
	public double PocketPresence { get; set; }
	public double DecisionMaking { get; set; }

	// Ball Carrying
	public double BallSecurity { get; set; }
	public double Vision { get; set; }
	public double Elusiveness { get; set; }
	public double BreakTackle { get; set; }

	// Receiving
	public double Hands { get; set; }
	public double RouteRunning { get; set; }
	public double Release { get; set; }
	public double ContestedCatch { get; set; }

	// Blocking
	public double PassBlock { get; set; }
	public double RunBlock { get; set; }
	public double LeadBlock { get; set; }

	// Defense
	public double PassRushPower { get; set; }
	public double PassRushFinesse { get; set; }
	public double BlockShedding { get; set; }
	public double Tackling { get; set; }
	public double Pursuit { get; set; }
	public double HitPower { get; set; }
	public double ManCoverage { get; set; }
	public double ZoneCoverage { get; set; }
	public double Press { get; set; }
	public double BallSkills { get; set; }

	// Special Teams
	public double KickPower { get; set; }
	public double KickAccuracy { get; set; }
	public double PuntPower { get; set; }
	public double PuntAccuracy { get; set; }
	public double Return { get; set; }
	public double SnapAccuracy { get; set; }

	#endregion

	// HiddenRatings' fields
	public double Potential { get; set; }
	public DevelopmentCurve DevelopmentCurve { get; set; }
	public double BustRisk { get; set; }
	public double PeakAgeOffset { get; set; }
	public double InjuryProneness { get; set; }

	// PersonalityTrait's members, in its order
	#region PersonalityTrait
	public double WorkEthic { get; set; }
	public double Coachability { get; set; }
	public double Greed { get; set; }
	public double Loyalty { get; set; }
	public double Competitiveness { get; set; }
	public double Ego { get; set; }

	#endregion
}