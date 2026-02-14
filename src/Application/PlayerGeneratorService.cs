using GridironFrontOffice.Application.Interfaces;
using GridironFrontOffice.Domain;
using GridironFrontOffice.Domain.Enums;
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

	public async Task<Player> GeneratePlayerAsync(PlayerPosition position, NamePool namePool = null)
	{
		if (namePool == null)
		{
			namePool = await _seedDataService.LoadNamePoolAsync();
		}

		var random = new Random();
		var firstName = namePool.FirstNames.ElementAt(random.Next(namePool.FirstNames.Count()));
		var lastName = namePool.LastNames.ElementAt(random.Next(namePool.LastNames.Count()));
		var college = SelectWeightedCollege(namePool.Colleges.ToList(), random);

		// Load Player archetypes for the given position
		var archetypesByPosition = await _seedDataService.LoadPlayerArchetypesAsync();
		var archetypesForPosition = archetypesByPosition.ContainsKey(position) ? archetypesByPosition[position] : new List<PlayerArchetype>();

		return new Player();
	}

	public async Task<IEnumerable<Player>> GeneratePlayersForTeamAsync(int teamID)
	{
		var namePool = await _seedDataService.LoadNamePoolAsync();

		// Generate a balanced team based on the defined position ranges
		var players = new List<Player>();

		foreach (var position in POSITION_RANGES.Keys)
		{
			var (min, max) = POSITION_RANGES[position];
			var count = new Random().Next(min, max + 1);

			for (int i = 0; i < count; i++)
			{
				var player = await GeneratePlayerAsync(position, namePool);
				players.Add(player);
			}
		}

		return players;
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