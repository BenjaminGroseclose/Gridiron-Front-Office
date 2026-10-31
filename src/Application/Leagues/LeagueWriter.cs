using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>
/// Writes a generated league into the save being created (D-089): an operation inside SaveManager's Save.Create
/// command, through the bulk insert (D-091). It assigns the IDs (WP5 §2): conferences, divisions and teams in the team
/// set's order, colleges in the list's, PlayerID = Slot + 1, contracts in PlayerID order.
/// </summary>
public static class LeagueWriter
{
	public static async Task WriteAsync(IUnitOfWork uow, GeneratedLeague league, ISeedData data, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(uow);
		ArgumentNullException.ThrowIfNull(league);
		ArgumentNullException.ThrowIfNull(data);

		// Parents before children: every connection has foreign keys on
		await uow.InsertAsync([new LeagueSeason { Season = league.Season, Cap = data.Contracts.Cap }], ct).ConfigureAwait(false);

		var conferences = new List<Conference>();
		var divisions = new List<Division>();
		var divisionOf = new Dictionary<string, DivisionID>();   // each team's division, by abbreviation

		for (var c = 0; c < data.TeamSet.Conferences.Count; c++)
		{
			var conference = data.TeamSet.Conferences[c];
			var conferenceID = new ConferenceID(c + 1);
			conferences.Add(new Conference { ConferenceID = conferenceID, Key = conference.Key, Name = conference.Name });

			foreach (var division in conference.Divisions)
			{
				var divisionID = new DivisionID(divisions.Count + 1);
				divisions.Add(new Division
				{
					DivisionID = divisionID, ConferenceID = conferenceID, Key = division.Key, Name = division.Name,
				});

				foreach (var abbr in division.Teams)
				{
					divisionOf.Add(abbr, divisionID);
				}
			}
		}

		await uow.InsertAsync(conferences, ct).ConfigureAwait(false);
		await uow.InsertAsync(divisions, ct).ConfigureAwait(false);

		// Teams are looked up by abbreviation and colleges by name: WP2's data tests keep both unique, and put every team
		// in exactly one division (TeamGuardrailTests), so a missing key can only be a broken team set
		var teamIDs = data.TeamSet.Teams
			.Select((team, i) => (team.Abbr, ID: new TeamID(i + 1)))
			.ToDictionary(x => x.Abbr, x => x.ID);
		await uow.InsertAsync(data.TeamSet.Teams.Select(t => new Team
		{
			TeamID = teamIDs[t.Abbr], DivisionID = divisionOf[t.Abbr],
			Abbreviation = t.Abbr, City = t.City, Nickname = t.Nickname,
			PrimaryColor = t.Colors.Primary, SecondaryColor = t.Colors.Secondary, AccentColor = t.Colors.Accent,
			CrestShape = t.Crest.Shape, CrestMonogram = t.Crest.Monogram, CrestStripe = t.Crest.Stripe,
			StadiumName = t.Stadium.Name, StadiumCapacity = t.Stadium.Capacity,
			StadiumRoof = t.Stadium.Roof, StadiumSurface = t.Stadium.Surface, StadiumClimate = t.Stadium.Climate,
		}), ct).ConfigureAwait(false);

		var collegeIDs = data.Colleges.Colleges
			.Select((college, i) => (college.Name, ID: new CollegeID(i + 1)))
			.ToDictionary(x => x.Name, x => x.ID);
		await uow.InsertAsync(data.Colleges.Colleges.Select(c => new College
		{
			CollegeID = collegeIDs[c.Name], Name = c.Name, Region = c.Region, Tier = c.Tier,
		}), ct).ConfigureAwait(false);

		// A player's team is the roster he's on: the cap trades moved some, and a slot keeps no team
		var players = league.Teams
			.SelectMany(t => t.Players.Select(player => (Generated: player, TeamID: (TeamID?)teamIDs[t.Definition.Abbr])))
			.Concat(league.FreeAgents.Select(player => (Generated: player, TeamID: (TeamID?)null)))
			.OrderBy(x => x.Generated.Slot)
			.ToList();
		await uow.InsertAsync(players.Select(x => x.Generated.ToPlayer(
			new PlayerID(x.Generated.Slot + 1),
			x.TeamID,
			x.TeamID is null ? PlayerStatus.FreeAgent : PlayerStatus.Active,
			collegeIDs[x.Generated.College.Name],
			x.Generated.Draft is { } draft ? teamIDs[draft.Team.Abbr] : null)), ct).ConfigureAwait(false);

		// Only rostered players have a contract, so each one has a team
		var contracts = new List<Contract>();
		var years = new List<ContractYear>();

		foreach (var (generated, teamID) in players.Where(x => x.Generated.Contract is not null))
		{
			var deal = generated.Contract!;
			var contractID = new ContractID(contracts.Count + 1);
			contracts.Add(new Contract
			{
				ContractID = contractID, PlayerID = new PlayerID(generated.Slot + 1), TeamID = teamID!.Value,
				Type = deal.Type, SignedSeason = deal.SignedSeason, Years = deal.Seasons.Count, IsActive = true,
				TotalValue = deal.TotalValue, SigningBonus = deal.SigningBonus, GuaranteedTotal = deal.GuaranteedTotal,
			});
			years.AddRange(deal.Seasons.Select(year => new ContractYear
			{
				ContractID = contractID, Season = year.Season, BaseSalary = year.BaseSalary,
				ProratedBonus = year.ProratedBonus, GuaranteedAmount = year.GuaranteedAmount, CapHit = year.CapHit,
			}));
		}

		await uow.InsertAsync(contracts, ct).ConfigureAwait(false);
		await uow.InsertAsync(years, ct).ConfigureAwait(false);

		// The league's position; SaveWriter saves the tracked change with the rest of the command
		var info = await uow.Db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
		info.Seed = league.Seed;
		info.CurrentSeason = league.Season;
		info.CurrentPhase = Phase.TrainingCamp;
		info.CurrentDate = LeagueCalendar.TrainingCampOpens(league.Season);
	}
}
