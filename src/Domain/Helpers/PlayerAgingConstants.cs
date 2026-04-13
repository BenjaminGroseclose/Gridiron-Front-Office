using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain.Helpers;

public static class PlayerAgingConstants
{
	public const int MIN_AGE = 20;
	public const int MAX_AGE = 40;
	public const double AGE_NOISE_STDDEV = 0.02;
	public const double AGE_MULTIPLIER_MIN = 0.6;
	public const double AGE_MULTIPLIER_MAX = 1.1;

	public static readonly string[] AgeCurveAttributes =
	[
		"Speed",
		"Strength",
		"Agility",
		"Awareness",
		"PlayRecognition",
		"Stamina",
		"Durability",
		"ThrowPower",
		"ShortAccuracy",
		"MediumAccuracy",
		"LongAccuracy",
		"BreakSack",
		"ThrowOnTheRun",
		"Mobility",
		"DecisionMakingSpeed",
		"Carrying",
		"Trucking",
		"Elusiveness",
		"Catching",
		"CatchInTraffic",
		"RouteRunning",
		"Release",
		"BallSecurity",
		"Hands",
		"JumpBallAbility",
		"Separation",
		"PassBlocking",
		"RunBlocking",
		"Pursuit",
		"Tackling",
		"RunDefense",
		"GapIntegrity",
		"BlockShedding",
		"PowerMoves",
		"FinesseMoves",
		"ManCoverage",
		"ZoneCoverage",
		"PressCoverage",
		"KickPower",
		"KickAccuracy"
	];

	public static readonly Dictionary<PlayerPosition, (int PeakStart, int PeakEnd, double MinMultiplier, double DeclinePerYear)> PositionAgeCurve =
		new()
	{
		{ PlayerPosition.QB, (27, 33, 0.75, 0.02) },
		{ PlayerPosition.RB, (23, 27, 0.72, 0.03) },
		{ PlayerPosition.WR, (24, 29, 0.74, 0.025) },
		{ PlayerPosition.TE, (25, 30, 0.75, 0.02) },
		{ PlayerPosition.OT, (26, 33, 0.78, 0.018) },
		{ PlayerPosition.OG, (26, 33, 0.78, 0.018) },
		{ PlayerPosition.C, (26, 33, 0.78, 0.018) },
		{ PlayerPosition.EDGE, (25, 30, 0.74, 0.025) },
		{ PlayerPosition.DT, (25, 31, 0.75, 0.023) },
		{ PlayerPosition.LB, (24, 30, 0.74, 0.025) },
		{ PlayerPosition.CB, (23, 28, 0.72, 0.028) },
		{ PlayerPosition.S, (24, 30, 0.74, 0.024) },
		{ PlayerPosition.K, (27, 35, 0.8, 0.015) },
		{ PlayerPosition.P, (27, 35, 0.8, 0.015) },
		{ PlayerPosition.LS, (27, 35, 0.8, 0.015) }
	};
}
