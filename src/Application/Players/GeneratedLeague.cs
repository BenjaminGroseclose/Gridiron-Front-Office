using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Players;

/// <summary>A whole generated league before it's saved (WP5 writes it in one command). Same seed and season, same league.</summary>
public sealed record GeneratedLeague(ulong Seed, int Season, IReadOnlyList<GeneratedTeam> Teams, IReadOnlyList<GeneratedPlayer> FreeAgents);

/// <summary>A team's 53 in depth order: positions in enum order, the generation tier's Starters first.</summary>
public sealed record GeneratedTeam(TeamDefinition Definition, IReadOnlyList<GeneratedPlayer> Players);


public sealed record GeneratedPlayer(
	int Slot,                      // his seed identity: Seeds.Derive(seed, SeedPurpose.Generation, season, Slot)
	Position Position,
	PlayerTier Tier,               // the generation tier that shaped his skills (F-03 §4.10)
	string Archetype,              // the profile drawn; for tests only, not saved (screens recompute the label)
	string FirstName,
	string LastName,
	DateOnly BirthDate,
	int Age,                       // on July 1 of the season (§2); not saved, the birthday is
	int Experience,
	CollegeDefinition College,
	Hometown Hometown,
	DraftInfo? Draft,              // null: undrafted
	int? JerseyNumber,             // null for free agents
	IReadOnlyDictionary<Measurable, double> Measurables,
	IReadOnlyDictionary<PlayerAttribute, double> Ratings,
	HiddenRatings Hidden,
	IReadOnlyDictionary<PersonalityTrait, double> Personality,
	GeneratedContract? Contract);  // null for free agents

/// <summary>Pick is the overall number (1 to rounds × teams); the pick within the round is derived.</summary>
public sealed record DraftInfo(int Season, int Round, int Pick, TeamDefinition Team);

public sealed record HiddenRatings(double Potential, DevelopmentCurve DevelopmentCurve, double BustRisk,
	double PeakAgeOffset, double InjuryProneness);

/// <summary>A contract with every season written, past ones included (D-074). Years = Seasons.Count.</summary>
public sealed record GeneratedContract(ContractType Type, int SignedSeason, long TotalValue, long SigningBonus,
	long GuaranteedTotal, IReadOnlyList<ContractSeason> Seasons);

public sealed record ContractSeason(int Season, long BaseSalary, long ProratedBonus, long GuaranteedAmount, long CapHit);