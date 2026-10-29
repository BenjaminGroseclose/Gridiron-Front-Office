using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Players;

public sealed class PlayerGenerator(ISeedData seedData)
{
	private readonly GenerationSettings _generation = seedData.Generation;
	private readonly Dictionary<Position, PositionProfile> _profiles = seedData.Positions.Positions.ToDictionary(x => x.Position);
	private readonly int _rookieYears = seedData.Contracts.RookieScale.Years;
	// The attributes the measurable maps feed (Speed, Acceleration, Agility, Strength), in enum order.
	private readonly PlayerAttribute[] _physical = seedData.Generation.Athletic.Ratings.Select(map => map.Attribute).Distinct().Order().ToArray();
	private readonly Dictionary<Position, WeightedPicker<Archetype>> _archetypes = seedData.Positions.Positions.ToDictionary(p => p.Position, p => new WeightedPicker<Archetype>(p.Archetypes.Select(a => (a, a.Share))));

	// The pickers below take their weights in enum order (OrderBy on the key), never in whatever order a dictionary holds.
	private readonly WeightedPicker<DevelopmentCurve> _developmentCurves = new(seedData.Generation.Hidden.DevelopmentCurve.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
	private readonly Dictionary<PlayerTier, WeightedPicker<CollegeTier>> _collegeTiers = seedData.Generation.CollegeTiers.ToDictionary(p => p.Key, p => new WeightedPicker<CollegeTier>(p.Value.OrderBy(s => s.Key).Select(s => (s.Key, s.Value))));
	// Each tier's colleges in the file's order.
	private readonly Dictionary<CollegeTier, List<CollegeDefinition>> _colleges = seedData.Colleges.Colleges.GroupBy(c => c.Tier).ToDictionary(g => g.Key, g => g.ToList());
	private readonly WeightedPicker<Hometown> _hometowns = new(seedData.Hometowns.Places.Select(h => (h, (double)h.Population)));


	private static readonly HashSet<Measurable> Drills = [Measurable.Forty, Measurable.TenSplit, Measurable.ThreeCone, Measurable.Shuttle, Measurable.Vertical, Measurable.BroadJump];
	private static readonly HashSet<Measurable> Timed = [Measurable.Forty, Measurable.TenSplit, Measurable.ThreeCone, Measurable.Shuttle];
	private static readonly HashSet<Measurable> Body = [Measurable.Height, Measurable.Weight, Measurable.ArmLength, Measurable.HandSize];


	/// <summary>
	/// One player, everything but his names, draft history, jersey and contract (the league fills those).
	/// <paramref name="teamStrength"/> is his team's place on the ladder (D-082); 0 for a free agent.
	/// </summary>
	public GeneratedPlayer Generate(ulong leagueSeed, int season, int slot, Position position, PlayerTier tier, double teamStrength)
	{
		var seed = Seeds.Derive(leagueSeed, SeedPurpose.Generation, season, slot);
		var random = new GfoRandom(seed);

		var profile = _profiles[position];
		var tierSettings = _generation.Tiers[tier];

		// Quality
		var quality = random.Normal();

		// Age
		var age = (int)Math.Round(random.Draw(profile.Age), MidpointRounding.AwayFromZero);

		// Birthday
		var start = new DateOnly(season, 7, 1);
		var birthDate = start.AddYears(-age).AddDays(-random.Next(0, 365));

		// Entry Age
		var entryAge = random.Next(_generation.Draft.EntryAge.Min, Math.Min(age, _generation.Draft.EntryAge.Max) + 1);
		var experience = age - entryAge;
		var strength = _generation.TeamStrength;
		quality += teamStrength * (experience < _rookieYears ? strength.YoungWeight : strength.VeteranWeight);

		// Athletic
		var athletic = random.Normal();
		var athleticSettings = _generation.Athletic;

		// Measurables
		var rho = athleticSettings.DrillCorrelation;
		var measurables = new Dictionary<Measurable, double>();

		foreach (var measurable in Enum.GetValues<Measurable>())
		{
			var epsilon = random.Normal();
			var z = Drills.Contains(measurable) ? Math.Sqrt(rho) * athletic + Math.Sqrt(1 - rho) * epsilon : epsilon;
			var shift = Body.Contains(measurable) ? 0 : tierSettings.AthleticShift;
			var sign = Timed.Contains(measurable) ? -1 : 1;

			var distribution = profile.Measurables[measurable];
			measurables[measurable] = distribution.Mean + sign * distribution.Sd * (z + shift);
		}

		// Physical Ratings
		var ratings = new Dictionary<PlayerAttribute, double>();
		foreach (var attribute in _physical)
		{
			var line = athleticSettings.Ratings
				.Where(map => map.Attribute == attribute)
				.Average(map => PhysicalLine(map, measurables[map.Measurable]));
			ratings[attribute] = Math.Clamp(line + random.Normal(0, athleticSettings.PhysicalNoiseSd), 0, 100);
		}

		// Archetype
		var archetype = _archetypes[position].Pick(random);

		// Skills
		var skills = _generation.Skills;
		foreach (var attribute in Enum.GetValues<PlayerAttribute>())
		{
			if (ratings.ContainsKey(attribute))
			{
				continue; // Skip the physical ratings
			}


			if (archetype.Skills.TryGetValue(attribute, out var mean) || skills.Common.TryGetValue(attribute, out mean))
			{
				var value = mean + tierSettings.SkillShift + quality * tierSettings.Spread * profile.SpreadScale + random.Normal(0, skills.NoiseSd);
				ratings[attribute] = Math.Clamp(value, 0, 100);
			}
			else
			{
				ratings[attribute] = random.Draw(skills.OffPosition);
			}

		}

		// Potential: the first growth band his age falls in (it draws even when its sd is 0)
		var hidden = _generation.Hidden;
		var overall = OverallCalculator.Overall(profile, ratings);
		var growth = hidden.PotentialGrowth.First(band => age <= band.ThroughAge).Growth;
		var potential = overall + random.Draw(growth);

		// Hidden
		var developmentCurve = _developmentCurves.Pick(random);
		var bustRisk = random.Draw(hidden.BustRisk);
		var peakAgeOffset = random.Draw(hidden.PeakAgeOffset);
		var injuryProneness = random.Draw(hidden.InjuryProneness);

		// Personality
		var personality = new Dictionary<PersonalityTrait, double>();
		foreach (var trait in Enum.GetValues<PersonalityTrait>())
		{
			personality[trait] = random.Draw(_generation.Personality.Value);
		}

		// College
		var collegeTier = _collegeTiers[tier].Pick(random);
		var colleges = _colleges[collegeTier];
		var college = colleges[random.Next(0, colleges.Count)];

		// Hometown
		var hometown = _hometowns.Pick(random);

		return new GeneratedPlayer(
			Slot: slot,
			Position: position,
			Tier: tier,
			Archetype: archetype.Name,
			FirstName: string.Empty,   // the names, draft, jersey and contract: the league fills them after the cap trades
			LastName: string.Empty,
			BirthDate: birthDate,
			Age: age,
			Experience: experience,
			College: college,
			Hometown: hometown,
			Draft: null,
			JerseyNumber: null,
			Measurables: measurables,
			Ratings: ratings,
			Hidden: new HiddenRatings(potential, developmentCurve, bustRisk, peakAgeOffset, injuryProneness),
			Personality: personality,
			Contract: null);
	}

	/// <summary>A measurable's straight line to a physical rating, through the map's best and worst points (no noise).</summary>
	public static double PhysicalLine(PhysicalRating map, double value)
	{
		ArgumentNullException.ThrowIfNull(map);
		var t = (value - map.Worst.Value) / (map.Best.Value - map.Worst.Value);
		return map.Worst.Rating + t * (map.Best.Rating - map.Worst.Rating);
	}
}
