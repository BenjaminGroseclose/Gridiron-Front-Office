using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain.Helpers;

public static class ContractHelpers
{
	private static readonly decimal LeagueAverageSalary = 5000000m;

	// Anchors for 2026: (First Pick Value, Last Pick Value, Total Picks in Round)
	private static readonly Dictionary<int, RoundBounds> RoundMap = new()
	{
		{ 1, new RoundBounds(1, 32, 54565500m, 16168614m) },
		{ 2, new RoundBounds(33, 64, 12937488m, 8550250m) },
		{ 3, new RoundBounds(65, 100, 7150000m, 6215000m) },
		{ 4, new RoundBounds(101, 135, 5850000m, 5100000m) },
		{ 5, new RoundBounds(136, 175, 5050000m, 4850000m) },
		{ 6, new RoundBounds(176, 220, 4800000m, 4550000m) },
		{ 7, new RoundBounds(221, 257, 4450000m, 4320000m) }
	};

	public static readonly Dictionary<PlayerPosition, decimal> PositionalWeight = new()
	{
		// Tier 1: Premium Impact (The "Money" Positions)
		{ PlayerPosition.QB, 1.25m },  // Franchise cornerstone
		{ PlayerPosition.OT, 1.15m },  // Blindside protection is vital
		{ PlayerPosition.EDGE, 1.15m }, // Pass rushers are high priority
		{ PlayerPosition.WR, 1.10m },  // Explosive playmakers

		// Tier 2: Standard Value (The Core Starters)
		{ PlayerPosition.CB, 1.05m },  // Lockdown corners are rising in value
		{ PlayerPosition.DT, 1.00m },  // Interior pass rushers higher, run stuffers lower
		{ PlayerPosition.C, 0.95m },   // Interior line generally slightly lower than OT
		{ PlayerPosition.OG, 0.95m },   

		// Tier 3: High Replaceability (The "Budget" Positions)
		{ PlayerPosition.S, 0.90m },   // Safety market has cooled recently
		{ PlayerPosition.LB, 0.85m },  // Off-ball linebackers 
		{ PlayerPosition.TE, 0.85m },  // Unless they are elite receivers
		{ PlayerPosition.RB, 0.80m },  // High turnover and short career spans

		// Tier 4: Specialists
		{ PlayerPosition.K, 0.70m },
		{ PlayerPosition.P, 0.70m },
		{ PlayerPosition.LS, 0.60m }
	};

	public static readonly decimal UndraftedFreeAgentValue = 2655000m; // 3 years at $1.3M per year (2026 minimum)

	// Max salary expressed as a multiple of a position's base value (positionWeight * LeagueAverageSalary).
	// Positions not listed fall back to 3x.
	private static readonly Dictionary<PlayerPosition, decimal> PositionalMaxMultiplier = new()
	{
		{ PlayerPosition.QB,   7.0m }, // Franchise QBs can command ~$43M in a $5M avg league
		{ PlayerPosition.OT,   5.0m },
		{ PlayerPosition.EDGE, 5.0m },
		{ PlayerPosition.WR,   4.5m },
		{ PlayerPosition.CB,   4.0m },
	};

	// Controls how steeply market value grows with OVR for each position tier.
	// Higher = more top-heavy market (elite talent commands a much larger premium).
	// Formula: e^(steepness × (OVR - 70)), anchored at 1.0× for an OVR 70 player.
	private static readonly Dictionary<PlayerPosition, double> RatingCurveSteepness = new()
	{
		// Franchise positions: elite talent commands a huge premium
		{ PlayerPosition.QB,   0.090 },
		{ PlayerPosition.EDGE, 0.080 },
		{ PlayerPosition.OT,   0.075 },

		// Core starters: meaningful but flatter premium for elite play
		{ PlayerPosition.WR,   0.070 },
		{ PlayerPosition.CB,   0.065 },
		{ PlayerPosition.DT,   0.060 },
		{ PlayerPosition.OG,   0.055 },
		{ PlayerPosition.C,    0.055 },
		{ PlayerPosition.TE,   0.055 },
		{ PlayerPosition.S,    0.055 },

		// High-replaceability: weak market response to elite OVR
		{ PlayerPosition.LB,   0.050 },
		{ PlayerPosition.RB,   0.045 },
		{ PlayerPosition.K,    0.040 },
		{ PlayerPosition.P,    0.040 },
		{ PlayerPosition.LS,   0.030 },
	};

	/// <summary>
	/// Calculates the minimum salary for a player based on their years of experience. 
	/// </summary>
	/// <param name="experienceYears"></param>
	/// <returns></returns>
	public static decimal GetMinimumSalaryForPosition(int experienceYears)
	{
		if (experienceYears == 0)
		{
			return 885000m; // Rookie minimum for undrafted players
		}

		if (experienceYears == 1)
		{
			return 1005000m;
		}

		if (experienceYears == 2)
		{
			return 1080000m;
		}

		if (experienceYears >= 3 && experienceYears < 7)
		{
			return 1155000m;
		}
		else // 7+ years of experience
		{
			return 1330000m;
		}
	}

	public static decimal GetRookieContractValue(int pick)
	{
		// Find the round this pick belongs to
		var roundData = RoundMap.Values.FirstOrDefault(r => pick >= r.StartPick && pick <= r.EndPick);

		// If pick is out of bounds (UDFA), return the standard 3-year min estimate
		if (roundData == null)
		{
			return UndraftedFreeAgentValue;
		}

		// 1. Calculate the 'progress' through the round (0.0 to 1.0)
		double progress = (double)(pick - roundData.StartPick) / (roundData.EndPick - roundData.StartPick);

		// 2. Linear Interpolation: StartValue - (TotalRoundDrop * progress)
		decimal totalDrop = roundData.StartValue - roundData.EndValue;
		decimal interpolatedValue = roundData.StartValue - (totalDrop * (decimal)progress);

		return Math.Round(interpolatedValue, 0);
	}

	public static decimal GetBaselineContractValue(int overallRating, int potentialRating, int experienceYears, PlayerPosition position)
	{
		// Default to a moderate weight if position is unknown
		var positionWeight = PositionalWeight.ContainsKey(position) ? PositionalWeight[position] : 0.85m;
		var baseValue = positionWeight * LeagueAverageSalary;

		// Exponential curve anchored at OVR 70 (≈ average generated player).
		// Steepness varies by position — elite QBs earn far more for their OVR than elite RBs.
		var steepness = RatingCurveSteepness.TryGetValue(position, out var s) ? s : 0.07;
		var ratingMultiplier = (decimal)Math.Exp(steepness * (overallRating - 70));
		var ageCurveMultiplier = GetAgeCurveMultiplier(experienceYears, position);

		// For simplicity, we'll assume all players are "Standard" archetypes with no bonus
		var archetypeBonus = 1.0m;
		var marketValue = baseValue * ratingMultiplier * ageCurveMultiplier * archetypeBonus;

		var maxMultiplier = PositionalMaxMultiplier.TryGetValue(position, out var mult) ? mult : 3.0m;
		return Math.Clamp(marketValue, GetMinimumSalaryForPosition(experienceYears), baseValue * maxMultiplier);
	}

	private static decimal GetAgeCurveMultiplier(int experienceYears, PlayerPosition position)
	{
		return position switch
		{
			// Longevity Kings: Peak later, slow decline
			PlayerPosition.QB or PlayerPosition.K or PlayerPosition.P => experienceYears switch
			{
				<= 3 => 0.90m,
				<= 10 => 1.00m, // Long peak
				<= 13 => 0.90m,
				_ => 0.80m      // Can still be effective at 15+ years
			},

			// The "Wall" Positions: Peak early, sharp decline
			PlayerPosition.RB or PlayerPosition.LB => experienceYears switch
			{
				<= 1 => 0.90m,
				<= 4 => 1.00m, // Peak is very early (Rookie contract)
				<= 6 => 0.85m, // Decline starts sooner
				_ => 0.65m      // The "cliff"
			},

			// Trench Warriors & WRs: Standard curve
			_ => experienceYears switch
			{
				<= 2 => 0.85m,
				<= 7 => 1.00m, // Prime years
				<= 10 => 0.80m,
				_ => 0.70m
			}
		};
	}

	private class RoundBounds
	{
		public int StartPick, EndPick;
		public decimal StartValue, EndValue;
		public RoundBounds(int s, int e, decimal sv, decimal ev)
		{
			StartPick = s;
			EndPick = e;
			StartValue = sv;
			EndValue = ev;
		}
	}
}