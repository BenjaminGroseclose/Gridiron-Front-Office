using System.Diagnostics.CodeAnalysis;

namespace GridironFrontOffice.Core.Players;

/// <summary>
/// A player's position (F-03 §4.1), in depth-chart order. Each name is its football abbreviation, so
/// position.ToString() is the label screens show. Saves store the name: rename only with a migration.
/// </summary>
public enum Position
{
	QB, RB, FB, WR, TE, OT, OG, C, EDGE, DT, LB, CB, S, K, P, LS,
}

/// <summary>
/// Where a player stands. TeamID follows it: set for Active and InjuredReserve; null for Prospect, FreeAgent and
/// Retired. (No practice squad in 1.0, D-080.)
/// </summary>
public enum PlayerStatus
{
	Prospect, Active, InjuredReserve, FreeAgent, Retired,
}

/// <summary>
/// The personaity traits F-03 §4.3 assigns to every player.
/// </summary>
public enum PersonalityTrait
{
	WorkEthic, Coachability, Greed, Loyalty, Competitiveness, Ego
}

/// <summary>
/// Potential as a letter grade (D-034; F-03 §4.6 maps peak OVR to it). Ordered worst to best, so comparisons read
/// naturally: grade >= PotentialGrade.B means "B or better". Screens show "A+", "A−" from a display map.
/// </summary>
public enum PotentialGrade
{
	F, D, CMinus, C, CPlus, BMinus, B, BPlus, AMinus, A, APlus,
}

/// <summary>The groups F-03 §4.2 sorts the attributes into; the Player page shows them in this order.</summary>
public enum AttributeGroup
{
	Physical, Mental, Passing, BallCarrying, Receiving, Blocking, Defense, SpecialTeams,
}

/// <summary>
/// The 42 player attributes (F-03 §4.2, D-050: every one is read by the sim), in that table's order. Each is also a
/// column in the Players table, so renaming one is a schema change.
/// </summary>
[SuppressMessage("Naming", "CA1711", Justification = "A football attribute (F-03's word for it), not a .NET attribute class.")]
public enum PlayerAttribute
{
	// Physical
	Speed, Acceleration, Agility, Strength, Stamina, Toughness,

	// Mental
	Awareness, Discipline,

	// Passing
	ArmStrength, ShortAccuracy, MediumAccuracy, DeepAccuracy, ThrowOnTheRun, PocketPresence, DecisionMaking,

	// Ball carrying
	BallSecurity, Vision, Elusiveness, BreakTackle,

	// Receiving
	Hands, RouteRunning, Release, ContestedCatch,

	// Blocking
	PassBlock, RunBlock, LeadBlock,

	// Defense
	PassRushPower, PassRushFinesse, BlockShedding, Tackling, Pursuit, HitPower, ManCoverage, ZoneCoverage, Press,
	BallSkills,

	// Special teams
	KickPower, KickAccuracy, PuntPower, PuntAccuracy, Return, SnapAccuracy,
}
