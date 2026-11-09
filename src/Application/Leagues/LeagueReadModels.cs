using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;

namespace GridironFrontOffice.Application.Leagues;

public sealed record TeamIdentityDto(TeamID TeamID, string Abbreviation, string City, string Nickname,
	string PrimaryColor, string SecondaryColor, string AccentColor, CrestShape CrestShape, string CrestMonogram,
	CrestStripe CrestStripe);

public sealed record TeamSummaryDto(TeamIdentityDto Identity, string ConferenceName, string DivisionName,
	string StadiumName, int StadiumCapacity, Roof StadiumRoof, PerceivedRosterRating RosterStrength,
	long Cap, long CapCommitted, long CapSpace);

public sealed record LeagueStructureDto(IReadOnlyList<ConferenceDto> Conferences);
public sealed record ConferenceDto(ConferenceID ConferenceID, string Key, string Name, IReadOnlyList<DivisionDto> Divisions);
public sealed record DivisionDto(DivisionID DivisionID, string Key, string Name, IReadOnlyList<TeamIdentityDto> Teams);

/// <summary>"Where am I": what the shell and Home show of the open league.</summary>
public sealed record LeagueHeaderDto(string LeagueName, ulong Seed, int Season, Phase Phase, DateOnly Date, TeamSummaryDto? UserTeam);
