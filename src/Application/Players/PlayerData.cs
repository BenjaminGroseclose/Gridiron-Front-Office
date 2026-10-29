using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Players;

// The built-in player data exactly as data/players, data/names and data/places hold it (M1 WP3 guide §2). Definitions,
// not read models, so no Dto suffix (D-070). Enum values and enum dictionary keys are the C# names; a field added later
// gets a default value, so older files still load (D-077). Ratings are on the internal 0–100 scale (F-03 §4.2).

/// <summary>data/players/positions.json: one profile per position.</summary>
public sealed record PositionData(IReadOnlyList<PositionProfile> Positions);

/// <summary>
/// What a position looks like: roster targets (F-03 §4.1), ages, the 11 measurables, the OVR weights (§4.5, a hand-set
/// first pass until SimLab replaces them in M2) and the archetypes a player draws from (D-078 #2). D-088 adds three:
/// <see cref="SpreadScale"/> times the tier's spread is how far the quality draw moves the position's skills (under 1
/// where OVR is one or two skills, which would spread it wider than the tier's target),
/// <see cref="EarliestRound"/> is the first draft round a player at the position goes in at creation, and
/// <see cref="DraftPenalty"/> is the OVR points taken off his draft score, so a held-back position spreads over the later
/// rounds instead of filling the first picks of its earliest one.
/// </summary>
public sealed record PositionProfile(
	Position Position,
	RosterTarget Roster,
	Distribution Age,
	IReadOnlyDictionary<Measurable, Distribution> Measurables,
	IReadOnlyDictionary<PlayerAttribute, double> OvrWeights,
	double SpreadScale,
	int EarliestRound,
	double DraftPenalty,
	IReadOnlyList<Archetype> Archetypes);

/// <summary>
/// The league <see cref="Average"/> of 53, a team's typical <see cref="Min"/>–<see cref="Max"/>, the <see cref="Floor"/>
/// every team keeps, and how many <see cref="Starters"/> the position fields.
/// </summary>
public sealed record RosterTarget(double Average, int Min, int Max, int Floor, int Starters);

/// <summary>A profile within a position: how often it appears, and each listed skill's mean for a Backup-tier player.</summary>
public sealed record Archetype(string Name, double Share, IReadOnlyDictionary<PlayerAttribute, double> Skills);

/// <summary>data/players/generation.json: the league-wide settings the generator (WP4) reads.</summary>
public sealed record GenerationSettings(
	RosterSizes Roster,
	IReadOnlyDictionary<PlayerTier, TierSettings> Tiers,
	SkillSettings Skills,
	AthleticSettings Athletic,
	HiddenSettings Hidden,
	PersonalitySettings Personality,
	IReadOnlyDictionary<PlayerTier, IReadOnlyDictionary<CollegeTier, double>> CollegeTiers,
	GenerationTargets Targets,
	DraftSettings Draft,
	TeamStrengthSettings TeamStrength);

/// <summary>The 53 and the unsigned pool (no practice squad in 1.0, D-080).</summary>
public sealed record RosterSizes(int Active, int FreeAgents, int MixSwaps);

/// <summary>How generated players got their draft history (D-081 #3): ranked entry classes, the top share drafted.</summary>
public sealed record DraftSettings(double DraftedShare, double RankNoiseSd, AgeRange EntryAge);
public sealed record AgeRange(int Min, int Max);

/// <summary>
/// The league's strength ladder (D-082): evenly spaced from −Range to +Range, in units of the quality draw. A player's
/// shift is his team's strength times YoungWeight within rookie-deal experience, else times VeteranWeight.
/// </summary>
public sealed record TeamStrengthSettings(double Range, double YoungWeight, double VeteranWeight);


/// <summary>How a tier shifts skills and athleticism, and the OVR distribution WP4's tests expect from it.</summary>
public sealed record TierSettings(double SkillShift, double Spread, double AthleticShift, Distribution Target);

/// <summary>Skill means every position shares, the off-position distribution, and the per-attribute noise.</summary>
public sealed record SkillSettings(IReadOnlyDictionary<PlayerAttribute, double> Common, Distribution OffPosition, double NoiseSd);

/// <summary>How the drills correlate, the noise on physical ratings, and the measurable-to-rating maps.</summary>
public sealed record AthleticSettings(double DrillCorrelation, double PhysicalNoiseSd, IReadOnlyList<PhysicalRating> Ratings);

/// <summary>The straight line from a measurable to a physical rating, through its best and worst points.</summary>
public sealed record PhysicalRating(Measurable Measurable, PlayerAttribute Attribute, RatingPoint Best, RatingPoint Worst);

public sealed record RatingPoint(double Value, double Rating);

/// <summary>The hidden ratings (F-03 §4.6): potential growth by age, the grade bands, and the per-player distributions.</summary>
public sealed record HiddenSettings(
	IReadOnlyList<PotentialGrowth> PotentialGrowth,
	IReadOnlyList<GradeBand> PotentialGrades,
	IReadOnlyDictionary<DevelopmentCurve, double> DevelopmentCurve,
	Distribution BustRisk,
	Distribution PeakAgeOffset,
	Distribution InjuryProneness);

/// <summary>The OVR growth a player of up to <see cref="ThroughAge"/> still has ahead.</summary>
public sealed record PotentialGrowth(int ThroughAge, Distribution Growth);

/// <summary>A potential grade and the lowest peak OVR that earns it.</summary>
public sealed record GradeBand(PotentialGrade Grade, double MinPeakOvr);

/// <summary>One distribution for all six traits, and the cut-offs for the Low and High descriptors (F-03 §4.7).</summary>
public sealed record PersonalitySettings(Distribution Value, double LowBelow, double HighAbove);

/// <summary>What WP4's distribution tests check (F-03 §4.10); not inputs to generation.</summary>
public sealed record GenerationTargets(CountRange Ovr90Plus, ValueTarget MeanAge, double RosterMixTolerance);

public sealed record CountRange(int Min, int Max);

public sealed record ValueTarget(double Value, double Tolerance);

/// <summary>data/players/jersey-rules.json: the NFL numbering rule by position (F-03 §4.8).</summary>
public sealed record JerseyRules(IReadOnlyList<JerseyRule> Rules);

public sealed record JerseyRule(Position Position, IReadOnlyList<NumberRange> Numbers);

/// <summary>An inclusive range of jersey numbers.</summary>
public sealed record NumberRange(int From, int To);

/// <summary>data/names/first.json and last.json: names weighted by how common they are.</summary>
public sealed record NameList(string Source, IReadOnlyList<WeightedName> Names);

public sealed record WeightedName(string Name, int Weight);

/// <summary>data/places/hometowns.json: places weighted by population.</summary>
public sealed record HometownList(string Source, IReadOnlyList<Hometown> Places);

public sealed record Hometown(string Name, string State, int Population);
