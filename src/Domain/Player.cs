using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Domain.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace GridironFrontOffice.Domain;

/// <summary>
/// Represents a player in the football simulation game. This class contains various properties that define the player's identity, physical attributes, skills, and other characteristics that can influence their performance on the field. The Player class is a central entity in the game, as it is used to create teams, simulate games, and manage player careers throughout the season.
/// 
/// Theory: Each player is assigned a BasePotential value (0 to 100) that represents long-term talent.
/// The player generation process uses BasePotential to bias archetype attribute means up or down.
/// 
/// Each player has position-specific "peak years". Ratings ramp up before peak, plateau during peak,
/// and decline gradually after peak. This creates realistic age-based curves while allowing outliers.
/// 
/// </summary>
public class Player : BaseEntity
{
	public int PlayerID { get; set; }

	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;

	/// <summary>
	/// Date of birth 
	/// </summary>
	public DateTime DateOfBirth { get; set; }

	/// <summary>
	/// Position the player plays on the field, which can influence their 
	/// attributes and play style.
	/// </summary>
	public PlayerPosition Position { get; set; }

	/// <summary>
	/// College the player attended
	/// </summary>
	public string College { get; set; } = string.Empty;

	/// <summary>
	/// Number of years the player has been in the league. 
	/// A rookie would have 0 years of experience, while a veteran might have 5 or more years.
	/// </summary>
	public int ExperienceYears { get; set; }

	/// <summary>
	/// The player's archetype, which can influence their attributes and play style. 
	/// For example, a QB archetype might be "Pocket Passer", "Dual Threat", or "Gunslinger". 
	/// </summary>
	public string Archetype { get; set; } = string.Empty;

	/// <summary>
	/// Base potential in the range 0 to 100.
	/// </summary>
	public int BasePotential { get; set; }

	/// <summary>
	/// Weight in pounds
	/// </summary>
	public int Weight { get; set; }

	/// <summary>
	/// Height in inches
	/// </summary>
	public double Height { get; set; }

	/// <summary>
	/// Jersey number of the player
	/// </summary>
	public int JerseyNumber { get; set; }


	// Player Personal Attributes
	public int Leadership { get; set; }
	public int WorkEthic { get; set; }
	public int FootballIQ { get; set; }
	public int Coachability { get; set; }
	public int Loyalty { get; set; }
	public int Discipline { get; set; }
	public int Competitiveness { get; set; }
	public int Consistency { get; set; }


	// Physical Attributes
	public int Speed { get; set; }
	public int Strength { get; set; }
	public int Agility { get; set; }
	public int Awareness { get; set; }
	public int PlayRecognition { get; set; }
	public int Stamina { get; set; }
	public int InjuryProneness { get; set; }
	public int Durability { get; set; }

	// QB Attributes
	public int ThrowPower { get; set; }
	public int ShortAccuracy { get; set; }
	public int MediumAccuracy { get; set; }
	public int LongAccuracy { get; set; }
	public int BreakSack { get; set; }
	public int ThrowOnTheRun { get; set; }
	public int Mobility { get; set; }
	public int DecisionMakingSpeed { get; set; }

	// WR / TE / RB Attributes
	public int Carrying { get; set; }
	public int Trucking { get; set; }
	public int Elusiveness { get; set; }
	public int Catching { get; set; }
	public int CatchInTraffic { get; set; }
	public int RouteRunning { get; set; }
	public int Release { get; set; }
	public int BallSecurity { get; set; }
	public int Hands { get; set; }
	public int JumpBallAbility { get; set; }
	public int Separation { get; set; }

	// OL Attributes
	public int PassBlocking { get; set; }
	public int RunBlocking { get; set; }

	// DEF Attributes
	public int Pursuit { get; set; }
	public int Tackling { get; set; }
	public int RunDefense { get; set; }
	public int GapIntegrity { get; set; }

	// DL / LB Attributes
	public int BlockShedding { get; set; }
	public int PowerMoves { get; set; }
	public int FinesseMoves { get; set; }

	// CB / S
	public int ManCoverage { get; set; }
	public int ZoneCoverage { get; set; }
	public int PressCoverage { get; set; }

	// K / P Attributes
	public int KickPower { get; set; }
	public int KickAccuracy { get; set; }

	[NotMapped]
	public int OverallRating => PlayerRatingHelper.CalculateOverallRating(this);
	[NotMapped]
	public bool IsRookie => ExperienceYears == 0;
	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	public override int ID
	{
		get => PlayerID;
	}
}