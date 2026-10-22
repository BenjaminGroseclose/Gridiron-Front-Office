using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Players;

public sealed class PlayerGenerator(ISeedData seedData, ILogger<PlayerGenerator> logger)
{
	private readonly GenerationSettings _generation = seedData.Generation;
	private readonly IReadOnlyDictionary<Position, PositionProfile> _profiles = seedData.Positions.Positions.ToDictionary(x => x.Position);
	private readonly int _rookieYears = seedData.Contracts.RookieScale.Years;


	/// <summary>
	/// One player, everything but his names, draft history, jersey and contract (the league fills those).
	/// <paramref name="teamStrength"/> is his team's place on the ladder (D-082); 0 for a free agent.
	/// </summary>
	public GeneratedPlayer Generate(ulong leagueSeed, int season, int slot, Position position, PlayerTier tier, double teamStrength)
	{
		var seed = new Seeds.Derive(leagueSeed, SeedPurpose.Generation, season, slot);
		var random = new GfoRandom(seed);

		var profile = _profiles[position];
		var tierSettings = _generation.Tiers[tier];

		// Quality
		var quality = random.Normal();

		// Age
		var age = random.Normal(profile.Age.Mean, profile.Age.Sd);

		// Birthday
		var start = new DateOnly(season, 7, 1);
		var birthDay = start.AddYears()

		// Entry Age

		// Athletic

		// Measurables

		//Physical Ratings

		// Archetype

		// Skills

		// Potential

		// Hidden

		// Personality

		// College

		// Hometown
	}

	/// <summary>A measurable's straight line to a physical rating, through the map's best and worst points (no noise).</summary>
	public static double PhysicalLine(PhysicalRating map, double value)
	{

	}

}