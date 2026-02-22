using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Domain.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace GridironFrontOffice.Domain;

public class Player : BaseEntity
{
	public int PlayerID { get; set; }

	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;

	/// <summary>
	/// Age of the player
	/// TODO: This should be calculated from DateOfBirth, based on the current date in the game
	/// </summary>
	public int Age { get; set; }

	/// <summary>
	/// Date of birth 
	/// </summary>
	public DateTime DateOfBirth { get; set; }

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
	/// Weight in pounds
	/// </summary>
	public int Weight { get; set; }

	/// <summary>
	/// Height in inches
	/// </summary>
	public int Height { get; set; }

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

	// WR/TE/RB Attributes
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