using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using GridironFrontOffice.Persistence.Models;

namespace GridironFrontOffice.Application;

public class PlayerGeneratorService : IPlayerGeneratorService
{
	#region Constants and Fields
	private const int BASE_POTENTIAL_MEAN = 50;
	private const int BASE_POTENTIAL_STDDEV = 15;
	private const int MIN_GENERATION_AGE = 20;
	private const int MAX_GENERATION_AGE = 40;
	private const double POTENTIAL_STDDEV_MULTIPLIER = 1.0;
	private const double AGE_NOISE_STDDEV = 0.02;
	private const double AGE_MULTIPLIER_MIN = 0.6;
	private const double AGE_MULTIPLIER_MAX = 1.1;

	private readonly string[] PLAYER_ATTRIBUTES =
	[
		// Player Personal Attributes
		"Leadership",
		"WorkEthic",
		"FootballIQ",
		"Coachability",
		"Loyalty",
		"Discipline",
		"Competitiveness",
		"Consistency",

		// Physical Attributes
		"Speed",
		"Strength",
		"Agility",
		"Awareness",
		"PlayRecognition",
		"Stamina",
		"InjuryProneness",
		"Durability",

		// QB Attributes
		"ThrowPower",
		"ShortAccuracy",
		"MediumAccuracy",
		"LongAccuracy",
		"BreakSack",
		"ThrowOnTheRun",
		"Mobility",
		"DecisionMakingSpeed",

		// WR / TE / RB Attributes
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

		// OL Attributes
		"PassBlocking",
		"RunBlocking",

		// DEF Attributes
		"Pursuit",
		"Tackling",
		"RunDefense",
		"GapIntegrity",

		// DL / LB Attributes
		"BlockShedding",
		"PowerMoves",
		"FinesseMoves",

		// CB / S Attributes
		"ManCoverage",
		"ZoneCoverage",
		"PressCoverage",

		// K / P Attributes
		"KickPower",
		"KickAccuracy"
	];

	private readonly string[] AGE_CURVE_ATTRIBUTES =
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

	private readonly Dictionary<string, int[]> GLOBAL_ATTRIBUTE_CONSTANTS = new Dictionary<string, int[]>
	{
		{ "Leadership", new int[] {70, 15} },
		{ "WorkEthic", new int[] {75, 10} },
		{ "FootballIQ", new int[] {70, 15} },
		{ "Coachability", new int[] {80, 10} },
		{ "Loyalty", new int[] {65, 20} },
		{ "Discipline", new int[] {70, 15} },
		{ "Competitiveness", new int[] {75, 10} },
		{ "Consistency", new int[] {70, 15} },
		{ "Stamina", new int[] {85, 7} },
		{ "InjuryProneness", new int[] {20, 12} },
		{ "Durability", new int[] {75, 10} },
		{ "PlayRecognition", new int[] {70, 15} }
	};


	private readonly Dictionary<PlayerPosition, (int min, int max)> POSITION_RANGES = new Dictionary<PlayerPosition, (int min, int max)>
	{
		{ PlayerPosition.QB, (2, 3) },
		{ PlayerPosition.RB, (3, 5) },
		{ PlayerPosition.WR, (5, 7) },
		{ PlayerPosition.TE, (3, 4) },
		{ PlayerPosition.OT, (4, 5) },
		{ PlayerPosition.OG, (4, 5) },
		{ PlayerPosition.C, (2, 4) },
		{ PlayerPosition.EDGE, (4, 6) },
		{ PlayerPosition.DT, (4, 6) },
		{ PlayerPosition.LB, (5, 8) },
		{ PlayerPosition.CB, (5, 7) },
		{ PlayerPosition.S, (4, 7) },
		{ PlayerPosition.K, (1, 1) },
		{ PlayerPosition.P, (1, 1) },
		{ PlayerPosition.LS, (1, 1) }
	};

	private readonly Dictionary<PlayerPosition, int[]> POSITION_WEIGHT_CONSTANTS = new Dictionary<PlayerPosition, int[]>
	{
		{ PlayerPosition.QB, new int[] { 222, 12 } },
		{ PlayerPosition.RB, new int[] { 214, 15 } },
		{ PlayerPosition.WR, new int[] { 201, 15 } },
		{ PlayerPosition.TE, new int[] { 254, 12 } },
		{ PlayerPosition.OT, new int[] { 315, 13 } },
		{ PlayerPosition.OG, new int[] { 312, 12 } },
		{ PlayerPosition.C, new int[] { 303, 8 } },
		{ PlayerPosition.EDGE, new int[] { 268, 13 } },
		{ PlayerPosition.DT, new int[] { 302, 15 } },
		{ PlayerPosition.LB, new int[] { 240, 9 } },
		{ PlayerPosition.CB, new int[] { 193, 10 } },
		{ PlayerPosition.S, new int[] { 208, 15 } },
		{ PlayerPosition.K, new int[] { 200, 5 } },
		{ PlayerPosition.P, new int[] { 200, 5 } },
		{ PlayerPosition.LS, new int[] { 230, 5 } }
	};

	private readonly Dictionary<PlayerPosition, double[]> POSITION_HEIGHT_CONSTANTS = new Dictionary<PlayerPosition, double[]>
	{
		{ PlayerPosition.QB, new double[] { 74.6, 1.9 } },
		{ PlayerPosition.RB, new double[] { 70.7, 2.1 } },
		{ PlayerPosition.WR, new double[] { 72.6, 2.4 } },
		{ PlayerPosition.TE, new double[] { 76.6, 1.4 } },
		{ PlayerPosition.OT, new double[] { 77.8, 1.2 } },
		{ PlayerPosition.OG, new double[] { 76.4, 1.3 } },
		{ PlayerPosition.C, new double[] { 75.8, 1.1 } },
		{ PlayerPosition.EDGE, new double[] { 76.2, 1.6 } },
		{ PlayerPosition.DT, new double[] { 75.3, 1.5 } },
		{ PlayerPosition.LB, new double[] { 74.3, 1.5 } },
		{ PlayerPosition.CB, new double[] { 71.9, 1.8 } },
		{ PlayerPosition.S, new double[] { 72.2, 1.7 } },
		{ PlayerPosition.K, new double[] { 70, 1.3 } },
		{ PlayerPosition.P, new double[] { 70, 1.3 } },
		{ PlayerPosition.LS, new double[] { 70, 1.3 } }
	};

	private readonly Dictionary<PlayerPosition, double[]> POSITION_AGE_CONSTANTS = new Dictionary<PlayerPosition, double[]>
	{
		{ PlayerPosition.QB, new double[] { 28.4, 4.2 } },
		{ PlayerPosition.RB, new double[] { 25.5, 1.8 } },
		{ PlayerPosition.WR, new double[] { 26.0, 2.4 } },
		{ PlayerPosition.TE, new double[] { 26.7, 2.9 } },
		{ PlayerPosition.OT, new double[] { 26.6, 2.5 } },
		{ PlayerPosition.OG, new double[] { 26.6, 2.5 } },
		{ PlayerPosition.C, new double[] { 26.6, 2.5 } },
		{ PlayerPosition.EDGE, new double[] { 27.0, 2.7 } },
		{ PlayerPosition.DT, new double[] { 27.0, 2.7 } },
		{ PlayerPosition.LB, new double[] { 26.3, 2.3 } },
		{ PlayerPosition.CB, new double[] { 25.9, 2.1 } },
		{ PlayerPosition.S, new double[] { 25.9, 2.1 } },
		{ PlayerPosition.K, new double[] { 29.4, 4.8 } },
		{ PlayerPosition.P, new double[] { 29.4, 4.8 } },
		{ PlayerPosition.LS, new double[] { 29.4, 4.8 } }
	};

	private readonly Dictionary<PlayerPosition, (int peakStart, int peakEnd, double minMultiplier, double declinePerYear)> POSITION_AGE_CURVE =
		new Dictionary<PlayerPosition, (int peakStart, int peakEnd, double minMultiplier, double declinePerYear)>
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
	#endregion

	private readonly ISeedDataService _seedDataService;

	public PlayerGeneratorService(ISeedDataService seedDataService)
	{
		_seedDataService = seedDataService;
	}

	/// <inheritdoc/>
	public async Task<Player> GeneratePlayerAsync(PlayerPosition position, NamePool namePool = null, Dictionary<PlayerPosition, List<PlayerArchetype>> archetypesByPosition = null, int age = 0)
	{
		if (namePool == null)
		{
			namePool = await _seedDataService.LoadNamePoolAsync();
		}

		if (archetypesByPosition == null)
		{
			archetypesByPosition = await _seedDataService.LoadPlayerArchetypesAsync();
		}

		var random = new Random();
		DateTime dateOfBirth;
		// If age is not provided, generate a random "rookie" age
		if (age <= 0)
		{
			age = random.Next(21, 25); // Generate a random "rookie" age between 21 and 24

		}
		age = NormalizeAge(age);
		dateOfBirth = DateTime.Now.AddYears(-age).AddDays(random.Next(0, 365));

		var basePotential = GenerateAttributeValue(BASE_POTENTIAL_MEAN, BASE_POTENTIAL_STDDEV, random);
		var firstName = namePool.FirstNames.ElementAt(random.Next(namePool.FirstNames.Count()));
		var lastName = namePool.LastNames.ElementAt(random.Next(namePool.LastNames.Count()));
		var college = SelectWeightedCollege(namePool.Colleges.ToList(), random);

		// Load Player archetypes for the given position

		var archetypesForPosition = archetypesByPosition.ContainsKey(position) ? archetypesByPosition[position] : new List<PlayerArchetype>();

		if (archetypesForPosition.Count == 0)
		{
			throw new DomainException($"No archetypes found for position {position}", "NO_ARCHETYPES_FOR_POSITION");
		}

		var playerArchetype = SelectWeightedArchetype(archetypesForPosition, random);

		var player = new Player
		{
			FirstName = firstName,
			LastName = lastName,
			Position = position,
			College = college,
			DateOfBirth = dateOfBirth,
			BasePotential = basePotential,
			Archetype = playerArchetype.Name,
			Weight = this.GenerateAttributeValue(POSITION_WEIGHT_CONSTANTS[position][0], POSITION_WEIGHT_CONSTANTS[position][1], random),
			Height = this.GenerateAttributeValue(POSITION_HEIGHT_CONSTANTS[position][0], POSITION_HEIGHT_CONSTANTS[position][1], random),
		};

		var archetypeAttributes = playerArchetype.Attributes;

		foreach (var attribute in PLAYER_ATTRIBUTES)
		{
			if (archetypeAttributes.ContainsKey(attribute))
			{
				var mean = archetypeAttributes[attribute][0];
				var stdDev = archetypeAttributes[attribute][1];
				var adjustedMean = AdjustMeanForPotential(mean, stdDev, basePotential);
				var value = this.GenerateAttributeValue(adjustedMean, stdDev, random);
				player.GetType().GetProperty(attribute).SetValue(player, value);
			}
			else if (GLOBAL_ATTRIBUTE_CONSTANTS.ContainsKey(attribute))
			{
				var mean = GLOBAL_ATTRIBUTE_CONSTANTS[attribute][0];
				var stdDev = GLOBAL_ATTRIBUTE_CONSTANTS[attribute][1];
				var adjustedMean = AdjustMeanForPotential(mean, stdDev, basePotential);
				var value = this.GenerateAttributeValue(adjustedMean, stdDev, random);
				player.GetType().GetProperty(attribute).SetValue(player, value);
			}
			else
			{
				// If no specific archetype or global constant is defined for this attribute, generate a random value with a default mean and stddev
				var adjustedMean = AdjustMeanForPotential(50, 15, basePotential);
				var value = this.GenerateAttributeValue(adjustedMean, 15, random); // Default mean of 50 and stddev of 15
				player.GetType().GetProperty(attribute).SetValue(player, value);
			}

			if (player.GetType().GetProperty(attribute).GetValue(player) == null)
			{
				throw new DomainException($"Attribute {attribute} was not set for player {player.FirstName} {player.LastName}", "ATTRIBUTE_NOT_SET");
			}
		}

		ApplyAgeCurveToAttributes(player, position, age, random);

		return player;
	}

	public async Task<IEnumerable<Player>> GeneratePlayersForTeamAsync(int teamID)
	{
		var namePool = await _seedDataService.LoadNamePoolAsync();
		var archetypesByPosition = await _seedDataService.LoadPlayerArchetypesAsync();

		// Generate a balanced team based on the defined position ranges
		var players = new List<Player>();
		var random = new Random();

		foreach (var position in POSITION_RANGES.Keys)
		{
			var (min, max) = POSITION_RANGES[position];
			var count = random.Next(min, max + 1);

			for (int i = 0; i < count; i++)
			{
				var age = GenerateAttributeValue(POSITION_AGE_CONSTANTS[position][0], POSITION_AGE_CONSTANTS[position][1], random);
				age = NormalizeAge(age);
				var player = await GeneratePlayerAsync(position, namePool, archetypesByPosition, age);
				players.Add(player);
			}
		}

		return players;
	}

	private int GenerateAttributeValue(double mean, double stdDev, Random random = null)
	{
		if (random == null)
		{
			random = new Random();
		}

		// Using Box-Muller transform to generate a normally distributed value
		double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] random doubles
		double u2 = 1.0 - random.NextDouble();
		double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
		int value = (int)(mean + stdDev * randStdNormal);
		return Math.Clamp(value, 1, 99); // Ensure attribute values are between 1 and 99
	}

	private double GenerateNormalValue(double mean, double stdDev, Random random)
	{
		// Using Box-Muller transform to generate a normally distributed value
		double u1 = 1.0 - random.NextDouble();
		double u2 = 1.0 - random.NextDouble();
		double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		return mean + stdDev * randStdNormal;
	}


	private int GenerateAttributeValue(int mean, int stdDev, Random random = null)
	{
		if (random == null)
		{
			random = new Random();
		}

		// Using Box-Muller transform to generate a normally distributed value
		double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] random doubles
		double u2 = 1.0 - random.NextDouble();
		double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
		int value = (int)(mean + stdDev * randStdNormal);
		return Math.Clamp(value, 1, 99); // Ensure attribute values are between 1 and 99
	}

	private int NormalizeAge(int age)
	{
		if (age <= 0)
		{
			return MIN_GENERATION_AGE;
		}

		return Math.Clamp(age, MIN_GENERATION_AGE, MAX_GENERATION_AGE);
	}

	private double AdjustMeanForPotential(double mean, double stdDev, int basePotential)
	{
		var potentialBias = (basePotential - 50) / 50.0;
		return mean + (stdDev * potentialBias * POTENTIAL_STDDEV_MULTIPLIER);
	}

	private void ApplyAgeCurveToAttributes(Player player, PlayerPosition position, int age, Random random)
	{
		var multiplier = GetAgeMultiplier(position, age, random);

		foreach (var attribute in AGE_CURVE_ATTRIBUTES)
		{
			var property = player.GetType().GetProperty(attribute);
			if (property == null || property.PropertyType != typeof(int))
			{
				continue;
			}

			var currentValue = (int)property.GetValue(player);
			var adjustedValue = (int)Math.Round(currentValue * multiplier);
			property.SetValue(player, Math.Clamp(adjustedValue, 1, 99));
		}
	}

	private double GetAgeMultiplier(PlayerPosition position, int age, Random random)
	{
		var (peakStart, peakEnd, minMultiplier, declinePerYear) = POSITION_AGE_CURVE[position];
		double multiplier;

		if (age <= peakStart)
		{
			var span = Math.Max(1, peakStart - MIN_GENERATION_AGE);
			var t = Math.Clamp((age - MIN_GENERATION_AGE) / (double)span, 0.0, 1.0);
			multiplier = minMultiplier + (t * (1.0 - minMultiplier));
		}
		else if (age <= peakEnd)
		{
			multiplier = 1.0;
		}
		else
		{
			multiplier = 1.0 - ((age - peakEnd) * declinePerYear);
			if (multiplier < minMultiplier)
			{
				multiplier = minMultiplier;
			}
		}

		multiplier += GenerateNormalValue(0, AGE_NOISE_STDDEV, random);
		return Math.Clamp(multiplier, AGE_MULTIPLIER_MIN, AGE_MULTIPLIER_MAX);
	}

	private PlayerArchetype SelectWeightedArchetype(List<PlayerArchetype> archetypes, Random random)
	{
		int totalWeight = archetypes.Sum(a => a.Weight);
		int roll = random.Next(0, totalWeight);
		int cursor = 0;

		foreach (var archetype in archetypes)
		{
			cursor += archetype.Weight;
			if (roll < cursor)
			{
				return archetype;
			}
		}
		return archetypes.Last(); // Fallback in case of rounding issues
	}


	private string SelectWeightedCollege(List<College> colleges, Random random)
	{
		int totalWeight = colleges.Sum(c => c.Weight);
		int roll = random.Next(0, totalWeight);
		int cursor = 0;

		foreach (var college in colleges)
		{
			cursor += college.Weight;
			if (roll < cursor)
			{
				return college.Name;
			}
		}
		return "Walk-on (No College)";
	}
}