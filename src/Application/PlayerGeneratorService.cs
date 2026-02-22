using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using GridironFrontOffice.Persistence.Models;

namespace GridironFrontOffice.Application;

public class PlayerGeneratorService : IPlayerGeneratorService
{
	private const int DEFAULT_PLAYERS_PER_TEAM = 53;

	private readonly Dictionary<string, int[]> GLOBAL_ATTRIBUTE_CONSTANTS = new Dictionary<string, int[]>
	{
		{ "Stamina", new int[] {85, 7} },
		{ "InjuryProneness", new int[] {20, 12} },
		{ "Durability", new int[] {75, 10} }
	};
	/*
	  "GlobalConstants": {
		"Stamina": [85, 7],
		"InjuryProneness": [20, 12],
		"Durability": [75, 10]
	  },
	*/

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

	private readonly ISeedDataService _seedDataService;

	public PlayerGeneratorService(ISeedDataService seedDataService)
	{
		_seedDataService = seedDataService;
	}

	public async Task<Player> GeneratePlayerAsync(PlayerPosition position, NamePool namePool = null, Dictionary<PlayerPosition, List<PlayerArchetype>> archetypesByPosition = null, int experienceYears = 0)
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
		var firstName = namePool.FirstNames.ElementAt(random.Next(namePool.FirstNames.Count()));
		var lastName = namePool.LastNames.ElementAt(random.Next(namePool.LastNames.Count()));
		var college = SelectWeightedCollege(namePool.Colleges.ToList(), random);

		// Load Player archetypes for the given position

		var archetypesForPosition = archetypesByPosition.ContainsKey(position) ? archetypesByPosition[position] : new List<PlayerArchetype>();

		if (archetypesForPosition.Count == 0)
		{
			throw new DomainException($"No archetypes found for position {position}", "NO_ARCHETYPES_FOR_POSITION", isFatal: true, retryable: false);
		}

		var playerArchetype = archetypesForPosition.FirstOrDefault((x) =>
		{
			var value = random.Next(0, 100);

			if (value < x.Weight)
			{
				return true;
			}
		});

		var player = new Player
		{
			FirstName = firstName,
			LastName = lastName,
			Position = position,
			College = college,
			Age = random.Next(21, 30), // Random age between 21 and 30
			ExperienceYears = experienceYears,
			Archetype = playerArchetype.Name,
			Height = random.Next(playerArchetype.HeightRange.Min(), playerArchetype.HeightRange.Max() + 1),
			Weight = random.Next(playerArchetype.WeightRange.Min(), playerArchetype.WeightRange.Max() + 1),

		};

		return player;
	}

	public async Task<IEnumerable<Player>> GeneratePlayersForTeamAsync(int teamID)
	{
		var namePool = await _seedDataService.LoadNamePoolAsync();
		var archetypesByPosition = await _seedDataService.LoadPlayerArchetypesAsync();

		// Generate a balanced team based on the defined position ranges
		var players = new List<Player>();

		foreach (var position in POSITION_RANGES.Keys)
		{
			var (min, max) = POSITION_RANGES[position];
			var count = new Random().Next(min, max + 1);

			for (int i = 0; i < count; i++)
			{
				// TODO: Consider passing in experience years based on some logic (e.g., rookies vs veterans) This should also affect their ratings and attributes.
				// A rookie should not be 99 overall, while a 10 year veteran might have higher attributes but lower physical traits. This will add more depth and realism to the player generation process.
				var experienceYears = new Random().Next(0, 10);
				var player = await GeneratePlayerAsync(position, namePool, archetypesByPosition, experienceYears);
				players.Add(player);
			}
		}

		return players;
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