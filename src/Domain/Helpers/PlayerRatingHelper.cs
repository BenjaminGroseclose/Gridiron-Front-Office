using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain.Helpers;

public static class PlayerRatingHelper
{
	/// <summary>
	/// Calculates overall rating based on position-specific attribute weighting
	/// </summary>
	public static int CalculateOverallRating(Player player)
	{
		return player.Position switch
		{
			PlayerPosition.QB => CalculateQBRating(player),
			PlayerPosition.RB => CalculateRBRating(player),
			PlayerPosition.WR => CalculateWRRating(player),
			PlayerPosition.TE => CalculateTERating(player),
			PlayerPosition.OT => CalculateOTRating(player),
			PlayerPosition.OG => CalculateOGRating(player),
			PlayerPosition.C => CalculateCenterRating(player),
			PlayerPosition.EDGE => CalculateEDGERating(player),
			PlayerPosition.DT => CalculateDTRating(player),
			PlayerPosition.LB => CalculateLBRating(player),
			PlayerPosition.CB => CalculateCBRating(player),
			PlayerPosition.S => CalculateSafetyRating(player),
			PlayerPosition.K => CalculateKickerRating(player),
			PlayerPosition.P => CalculatePunterRating(player),
			_ => CalculateDefaultRating(player)
		};
	}

	private static int CalculateQBRating(Player player)
	{
		// QB priorities: Accuracy, Arm Strength, Decision Making, Mobility, Evasion
		int weighted = (player.ShortAccuracy * 22) +
					   (player.MediumAccuracy * 18) +
					   (player.LongAccuracy * 18) +
					   (player.ThrowPower * 14) +
					   (player.DecisionMakingSpeed * 10) +
					   (player.FootballIQ * 10) +
					   (player.Awareness * 8) +
					   (player.Mobility * 8) +
					   (player.BreakSack * 6) +
					   (player.Competitiveness * 4) +
					   (player.Stamina * 4);
		return weighted / 122;
	}

	private static int CalculateRBRating(Player player)
	{
		// RB priorities: Speed, Elusiveness, Carrying, Ball Security, Agility, Competitiveness
		int weighted = (player.Speed * 20) +
					   (player.Elusiveness * 18) +
					   (player.Carrying * 16) +
					   (player.BallSecurity * 14) +
					   (player.Agility * 16) +
					   (player.Awareness * 8) +
					   (player.Catching * 6) +
					   (player.Competitiveness * 4) +
					   (player.Consistency * 2);
		return weighted / 104;
	}

	private static int CalculateWRRating(Player player)
	{
		// WR priorities: Catching, Route Running, Speed, Hands, Separation, Jump Ball Ability
		int weighted = (player.Catching * 18) +
					   (player.Hands * 12) +
					   (player.RouteRunning * 18) +
					   (player.Speed * 16) +
					   (player.Release * 12) +
					   (player.Separation * 10) +
					   (player.Agility * 10) +
					   (player.JumpBallAbility * 8) +
					   (player.CatchInTraffic * 8) +
					   (player.Consistency * 4) +
					   (player.Stamina * 2);
		return weighted / 118;
	}

	private static int CalculateTERating(Player player)
	{
		// TE priorities: Catching, Route Running, Blocking, Hands, Speed, Strength, Jump Ball Ability
		int weighted = (player.Catching * 16) +
					   (player.Hands * 10) +
					   (player.RouteRunning * 14) +
					   (player.PassBlocking * 14) +
					   (player.Speed * 12) +
					   (player.Strength * 10) +
					   (player.CatchInTraffic * 10) +
					   (player.JumpBallAbility * 8) +
					   (player.BallSecurity * 6) +
					   (player.Consistency * 4) +
					   (player.Stamina * 6);
		return weighted / 110;
	}

	private static int CalculateOTRating(Player player)
	{
		// OT (Offensive Tackle) priorities: Pass Blocking, Run Blocking, Strength, Discipline, Consistency
		int weighted = (player.PassBlocking * 28) +
					   (player.RunBlocking * 24) +
					   (player.Strength * 20) +
					   (player.Discipline * 8) +
					   (player.Agility * 10) +
					   (player.Awareness * 6) +
					   (player.Consistency * 4);
		return weighted / 100;
	}

	private static int CalculateOGRating(Player player)
	{
		// OG (Offensive Guard) priorities: Pass Blocking, Run Blocking, Strength, Discipline, Consistency
		int weighted = (player.PassBlocking * 26) +
					   (player.RunBlocking * 28) +
					   (player.Strength * 26) +
					   (player.Discipline * 8) +
					   (player.Awareness * 8) +
					   (player.Consistency * 4);
		return weighted / 100;
	}

	private static int CalculateCenterRating(Player player)
	{
		// Center priorities: Pass Blocking, Run Blocking, Strength, FootballIQ, Discipline, Consistency
		int weighted = (player.PassBlocking * 24) +
					   (player.RunBlocking * 26) +
					   (player.Strength * 24) +
					   (player.FootballIQ * 10) +
					   (player.Discipline * 8) +
					   (player.Awareness * 6) +
					   (player.Consistency * 2);
		return weighted / 100;
	}

	private static int CalculateEDGERating(Player player)
	{
		// EDGE (DE/OLB) priorities: Speed, Strength, Pursuit, Block Shedding, Power Moves, Discipline, Competitiveness
		int weighted = (player.Speed * 16) +
					   (player.Strength * 18) +
					   (player.Pursuit * 16) +
					   (player.BlockShedding * 18) +
					   (player.PowerMoves * 13) +
					   (player.Agility * 8) +
					   (player.Discipline * 6) +
					   (player.Competitiveness * 4) +
					   (player.Durability * 5);
		return weighted / 104;
	}

	private static int CalculateDTRating(Player player)
	{
		// DT (Defensive Tackle) priorities: Strength, Block Shedding, Pursuit, Power Moves, Gap Integrity, Discipline
		int weighted = (player.Strength * 26) +
					   (player.BlockShedding * 24) +
					   (player.GapIntegrity * 14) +
					   (player.Pursuit * 16) +
					   (player.PowerMoves * 16) +
					   (player.Discipline * 6) +
					   (player.Durability * 4);
		return weighted / 106;
	}

	private static int CalculateLBRating(Player player)
	{
		// LB (Linebacker) priorities: Tackling, Pursuit, Awareness, PlayRecognition, Block Shedding, Gap Integrity, Discipline
		int weighted = (player.Tackling * 20) +
					   (player.Pursuit * 18) +
					   (player.Awareness * 16) +
					   (player.PlayRecognition * 14) +
					   (player.BlockShedding * 11) +
					   (player.GapIntegrity * 10) +
					   (player.Speed * 8) +
					   (player.Discipline * 6) +
					   (player.Competitiveness * 3);
		return weighted / 106;
	}

	private static int CalculateCBRating(Player player)
	{
		// CB (Cornerback) priorities: Man Coverage, Press Coverage, Speed, Agility, Awareness, Consistency, Competitiveness
		int weighted = (player.ManCoverage * 22) +
					   (player.PressCoverage * 18) +
					   (player.Speed * 18) +
					   (player.Agility * 16) +
					   (player.Awareness * 11) +
					   (player.PlayRecognition * 7) +
					   (player.Consistency * 4) +
					   (player.Competitiveness * 4) +
					   (player.Discipline * 2);
		return weighted / 102;
	}

	private static int CalculateSafetyRating(Player player)
	{
		// Safety priorities: Zone Coverage, Awareness, PlayRecognition, Speed, Tackling, Run Defense, Discipline
		int weighted = (player.ZoneCoverage * 20) +
					   (player.Awareness * 20) +
					   (player.PlayRecognition * 18) +
					   (player.Speed * 14) +
					   (player.Tackling * 12) +
					   (player.RunDefense * 10) +
					   (player.Discipline * 4) +
					   (player.Durability * 2);
		return weighted / 100;
	}

	private static int CalculateKickerRating(Player player)
	{
		// K (Kicker) priorities: Kick Power, Kick Accuracy, FootballIQ
		int weighted = (player.KickPower * 40) +
					   (player.KickAccuracy * 50) +
					   (player.FootballIQ * 10);
		return weighted / 100;
	}

	private static int CalculatePunterRating(Player player)
	{
		// P (Punter) priorities: Kick Power, Kick Accuracy, FootballIQ, Stamina
		int weighted = (player.KickPower * 38) +
					   (player.KickAccuracy * 48) +
					   (player.FootballIQ * 8) +
					   (player.Stamina * 6);
		return weighted / 100;
	}

	private static int CalculateDefaultRating(Player player)
	{
		// Fallback calculation for unknown positions
		int weighted = (player.Speed * 14) +
					   (player.Strength * 14) +
					   (player.Agility * 14) +
					   (player.Awareness * 14) +
					   (player.PlayRecognition * 14) +
					   (player.Stamina * 8) +
					   (player.FootballIQ * 8) +
					   (player.Tackling * 5) +
					   (player.Consistency * 2) +
					   (player.Competitiveness * 1);
		return weighted / 94;
	}
}