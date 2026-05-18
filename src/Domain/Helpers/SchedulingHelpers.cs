using GridironFrontOffice.Domain.Enums;

namespace Domain.Helpers;

public static class SchedulingHelpers
{
	/// <summary>
	/// The weeks during which teams are eligible to have a bye week.
	/// </summary>
	public static readonly int[] BYE_WEEKS = { 5, 6, 7, 8, 9, 10, 11, 12 };


	// ---------------------------------------------------------------------------
	// Rotation tables
	// ---------------------------------------------------------------------------

	public static Division GetIntraRotation(Division division, int season)
	{
		return (division, season % 3) switch
		{
			(Division.North, 0) => Division.South,
			(Division.North, 1) => Division.East,
			(Division.North, 2) => Division.West,
			(Division.South, 0) => Division.East,
			(Division.South, 1) => Division.West,
			(Division.South, 2) => Division.North,
			(Division.East, 0) => Division.West,
			(Division.East, 1) => Division.North,
			(Division.East, 2) => Division.South,
			(Division.West, 0) => Division.North,
			(Division.West, 1) => Division.South,
			(Division.West, 2) => Division.East,
			_ => throw new Exception($"No intra-conference rotation defined for division {division} and season {season}")
		};
	}

	public static Division GetInterRotation(Division division, int season)
	{
		return (division, season % 4) switch
		{
			(Division.North, 0) => Division.North,
			(Division.North, 1) => Division.South,
			(Division.North, 2) => Division.East,
			(Division.North, 3) => Division.West,
			(Division.South, 0) => Division.South,
			(Division.South, 1) => Division.East,
			(Division.South, 2) => Division.West,
			(Division.South, 3) => Division.North,
			(Division.East, 0) => Division.East,
			(Division.East, 1) => Division.West,
			(Division.East, 2) => Division.North,
			(Division.East, 3) => Division.South,
			(Division.West, 0) => Division.West,
			(Division.West, 1) => Division.North,
			(Division.West, 2) => Division.South,
			(Division.West, 3) => Division.East,
			_ => throw new Exception($"No inter-conference rotation defined for division {division} and season {season}")
		};
	}

	/// <summary>
	/// Returns the opposite-conference division for the 17th game.
	/// The mapping is always symmetric (if North→South then South→North)
	/// and never overlaps with the inter-conference rotation.
	/// </summary>
	public static Division GetSeventeenthGameRotation(Division division, int season)
	{
		return (division, season % 4) switch
		{
			// s%4=0: N↔S, E↔W  (inter is identity — no overlap)
			(Division.North, 0) => Division.South,
			(Division.South, 0) => Division.North,
			(Division.East, 0) => Division.West,
			(Division.West, 0) => Division.East,
			// s%4=1: N↔E, S↔W  (inter is N→S,S→E,E→W,W→N — no overlap)
			(Division.North, 1) => Division.East,
			(Division.South, 1) => Division.West,
			(Division.East, 1) => Division.North,
			(Division.West, 1) => Division.South,
			// s%4=2: N↔S, E↔W  (inter is N→E,S→W,E→N,W→S — no overlap)
			(Division.North, 2) => Division.South,
			(Division.South, 2) => Division.North,
			(Division.East, 2) => Division.West,
			(Division.West, 2) => Division.East,
			// s%4=3: N↔E, S↔W  (inter is N→W,S→N,E→S,W→E — no overlap)
			(Division.North, 3) => Division.East,
			(Division.South, 3) => Division.West,
			(Division.East, 3) => Division.North,
			(Division.West, 3) => Division.South,
			_ => throw new Exception($"No 17th-game rotation defined for division {division} and season {season}")
		};
	}



}

// ---------------------------------------------------------------------------
// Inner types
// ---------------------------------------------------------------------------

public enum MatchupType
{
	Divisional,
	IntraConference,
	InterConference,
	SameRankIntraConference,
	SameRankInterConference
}