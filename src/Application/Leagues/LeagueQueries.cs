using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>
/// The league's read models (M1 WP6 §4.2): the header, the structure, the team identities and the team summaries, each
/// through the user's eyes (SaveInfo.UserTeamID, null before the pick).
/// </summary>
public sealed class LeagueQueries(ISaveReader reader, IPerception perception)
{
	/// <summary>The league's name, season, phase and date, and the user team's summary once it's picked.</summary>
	public Task<LeagueHeaderDto> GetHeaderAsync(CancellationToken cancellationToken = default)
		=> reader.QueryAsync(async (db, ct) =>
		{
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);

			// The user's team is always in the save, so Single
			var userTeam = saveInfo.UserTeamID is { } team
				? (await SummariesAsync(db, saveInfo, team, ct).ConfigureAwait(false)).Single()
				: null;

			return new LeagueHeaderDto(saveInfo.LeagueName, saveInfo.Seed, saveInfo.CurrentSeason, saveInfo.CurrentPhase,
				saveInfo.CurrentDate, userTeam);
		}, cancellationToken);

	/// <summary>
	/// The conferences, their divisions and their teams, each in ID order: the team set's order (WP6 §2). The three
	/// reads sort in SQL, since a typed ID can't be compared in memory (§8); Where keeps that order.
	/// </summary>
	public async Task<LeagueStructureDto> GetStructureAsync(CancellationToken cancellationToken = default)
		=> await reader.QueryAsync(async (db, ct) =>
		{
			var conferences = await db.Conferences.OrderBy(c => c.ConferenceID).ToListAsync(ct).ConfigureAwait(false);
			var divisions = await db.Divisions.OrderBy(d => d.DivisionID).ToListAsync(ct).ConfigureAwait(false);
			var teams = await db.Teams.OrderBy(t => t.TeamID).ToListAsync(ct).ConfigureAwait(false);

			var conferenceDtos = conferences.Select(c => new ConferenceDto(
				c.ConferenceID,
				c.Key,
				c.Name,
				divisions.Where(d => d.ConferenceID == c.ConferenceID).Select(d => new DivisionDto(
					d.DivisionID,
					d.Key,
					d.Name,
					teams.Where(t => t.DivisionID == d.DivisionID).Select(ToIdentity).ToList()
				)).ToList()
			)).ToList();

			return new LeagueStructureDto(conferenceDtos);
		}, cancellationToken).ConfigureAwait(false);

	/// <summary>Every team's identity in TeamID order, read each time (D-093 #3).</summary>
	public async Task<IReadOnlyList<TeamIdentityDto>> GetTeamIdentitiesAsync(CancellationToken cancellationToken = default)
		=> await reader.QueryAsync(async (db, ct) =>
		{
			var teams = await db.Teams.OrderBy(t => t.TeamID).ToListAsync(ct).ConfigureAwait(false);
			return teams.Select(ToIdentity).ToList();
		}, cancellationToken).ConfigureAwait(false);

	/// <summary>Every team's summary in TeamID order, for WP8's picker.</summary>
	public Task<IReadOnlyList<TeamSummaryDto>> GetTeamSummariesAsync(CancellationToken cancellationToken = default)
		=> reader.QueryAsync(async (db, ct) =>
		{
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
			return await SummariesAsync(db, saveInfo, null, ct).ConfigureAwait(false);
		}, cancellationToken);

	/// <summary>One team's summary, or null when there's no such team.</summary>
	public Task<TeamSummaryDto?> GetTeamSummaryAsync(TeamID team, CancellationToken cancellationToken = default)
		=> reader.QueryAsync(async (db, ct) =>
		{
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
			return (await SummariesAsync(db, saveInfo, team, ct).ConfigureAwait(false)).SingleOrDefault();
		}, cancellationToken);

	/// <summary>
	/// Every team's summary in TeamID order, or only <paramref name="only"/>'s: four statements however many teams.
	/// The user's team is the viewer for every roster (null before the pick), so rivals show through the fog (D-032).
	/// </summary>
	private async Task<IReadOnlyList<TeamSummaryDto>> SummariesAsync(ILeagueReadDb db, SaveInfo saveInfo, TeamID? only,
		CancellationToken ct)
	{
		var teams = await db.Teams
			.Where(t => only == null || t.TeamID == only)
			.Join(db.Divisions, t => t.DivisionID, d => d.DivisionID, (t, d) => new { Team = t, Division = d })
			.Join(db.Conferences, x => x.Division.ConferenceID, c => c.ConferenceID,
				(x, c) => new { x.Team, Division = x.Division.Name, Conference = c.Name })
			.OrderBy(x => x.Team.TeamID)
			.ToListAsync(ct)
			.ConfigureAwait(false);

		var cap = await db.Seasons.Where(s => s.Season == saveInfo.CurrentSeason).Select(s => s.Cap).SingleAsync(ct)
			.ConfigureAwait(false);

		// This season's cap hit of every active contract, summed by team in SQL
		var committed = await db.Contracts
			.Where(c => c.IsActive && (only == null || c.TeamID == only))
			.Join(db.ContractYears.Where(y => y.Season == saveInfo.CurrentSeason),
				c => c.ContractID, y => y.ContractID, (c, y) => new { c.TeamID, y.CapHit })
			.GroupBy(x => x.TeamID)
			.Select(g => new { TeamID = g.Key, CapHit = g.Sum(x => x.CapHit) })
			.ToDictionaryAsync(x => x.TeamID, x => x.CapHit, ct)
			.ConfigureAwait(false);

		// Every rostered player in one read, split by team in memory
		var players = await db.Players
			.Where(p => p.Status == PlayerStatus.Active && p.TeamID != null && (only == null || p.TeamID == only))
			.ToListAsync(ct)
			.ConfigureAwait(false);
		var rosters = players.ToLookup(p => p.TeamID!.Value, p => p.ToSnapshot());

		return [.. teams.Select(x =>
		{
			var strength = perception.PerceiveRoster(saveInfo.UserTeamID, [.. rosters[x.Team.TeamID]]);
			var spent = committed.GetValueOrDefault(x.Team.TeamID);
			return new TeamSummaryDto(ToIdentity(x.Team), x.Conference, x.Division, x.Team.StadiumName,
				x.Team.StadiumCapacity, x.Team.StadiumRoof, strength, cap, spent, cap - spent);
		})];
	}

	/// <summary>A team's identity, built the same way for the header, the structure and the summaries.</summary>
	private static TeamIdentityDto ToIdentity(Team t) =>
		new(t.TeamID, t.Abbreviation, t.City, t.Nickname, t.PrimaryColor, t.SecondaryColor, t.AccentColor,
			t.CrestShape, t.CrestMonogram, t.CrestStripe);
}
