using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Players;

public readonly record struct PotentialRange(PotentialGrade Grade, PotentialGrade Low, PotentialGrade High);

public sealed record PlayerRowDto(PlayerID PlayerID, string FirstName, string LastName, Position Position,
	int? JerseyNumber, int Age, TeamID? TeamID, PerceivedRating Overall, PotentialRange Potential, string Archetype,
	long? CapHit);   // CapHit: this season's, null for a free agent

public sealed record PlayerDetailDto(PlayerRowDto Row, DateOnly BirthDate, string College, string HometownName,
	string HometownState, int Experience, DraftInfoDto? Draft,
	IReadOnlyDictionary<Measurable, double> Measurables,
	IReadOnlyDictionary<PlayerAttribute, PerceivedRating> Attributes,
	IReadOnlyDictionary<PersonalityTrait, TraitLevel>? Personality,   // null: "Unknown"
	ContractSummaryDto? Contract);                                    // null for a free agent

public sealed record DraftInfoDto(int Season, int Round, int Pick, TeamID Team);   // null when undrafted

public sealed record ContractSummaryDto(ContractType Type, int SignedSeason, int Years, long TotalValue, long Apy,
	long CapHit, int YearsLeft, long GuaranteedLeft, IReadOnlyList<ContractYearDto> Seasons);

public sealed record ContractYearDto(int Season, long BaseSalary, long ProratedBonus, long GuaranteedAmount, long CapHit);
