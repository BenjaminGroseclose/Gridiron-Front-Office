using System.Globalization;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Players;

/// <summary>
/// The players' read models (M1 WP6 §4.2): a roster, the league's list and one player's detail, each through the user's
/// eyes (SaveInfo.UserTeamID, null before the pick). Every row is built by one helper, so the three agree.
/// </summary>
public sealed class PlayerQueries(ISaveReader reader, IPerception perception, ISeedData data, TimeProvider time,
	ILogger<PlayerQueries> log)
{
	private readonly Dictionary<Position, PositionProfile> _profiles = data.Positions.Positions.ToDictionary(p => p.Position);

	/// <summary>
	/// A team's active players, by position (the enum's order), then OVR estimate descending, then last and first name
	/// (WP6 §2). OVR isn't a column, so the sort is in memory, on the estimate: a PerceivedRating doesn't compare (§8).
	/// </summary>
	public async Task<IReadOnlyList<PlayerRowDto>> GetRosterAsync(TeamID team, CancellationToken cancellationToken = default)
		=> await reader.QueryAsync(async (db, ct) =>
		{
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
			var players = await db.Players.Where(p => p.TeamID == team && p.Status == PlayerStatus.Active).ToListAsync(ct)
				.ConfigureAwait(false);

			// This season's cap hit of each player's active contract
			var capHits = await db.Contracts
				.Where(c => c.IsActive && c.TeamID == team)
				.Join(db.ContractYears.Where(y => y.Season == saveInfo.CurrentSeason),
					c => c.ContractID, y => y.ContractID, (c, y) => new { c.PlayerID, y.CapHit })
				.ToDictionaryAsync(x => x.PlayerID, x => (long?)x.CapHit, ct)   // nullable, so a free agent's is null, not 0
				.ConfigureAwait(false);

			return players.Select(p => ToPlayerRowDto(p, perception.Perceive(saveInfo.UserTeamID, p.ToSnapshot()),
				saveInfo.CurrentDate, capHits.GetValueOrDefault(p.PlayerID)))
				.OrderBy(r => r.Position)
				.ThenByDescending(r => r.Overall.Estimate)
				.ThenBy(r => r.LastName, StringComparer.Ordinal)
				.ThenBy(r => r.FirstName, StringComparer.Ordinal)
				.ToList();
		}, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Every player who isn't retired, rostered or free agent, in PlayerID order: WP9's Roster grid. Logs 1306 with the
	/// time the reads and the perception took (F-03 §12).
	/// </summary>
	public async Task<IReadOnlyList<PlayerRowDto>> GetLeaguePlayersAsync(CancellationToken cancellationToken = default)
		=> await reader.QueryAsync(async (db, ct) =>
		{
			var started = time.GetTimestamp();
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);

			// Sorted in SQL: a typed ID can't be compared in memory (§8)
			var players = await db.Players
				.Where(p => p.Status != PlayerStatus.Retired)
				.OrderBy(p => p.PlayerID)
				.ToListAsync(ct)
				.ConfigureAwait(false);

			var capHits = await db.Contracts
				.Where(c => c.IsActive)
				.Join(db.ContractYears.Where(y => y.Season == saveInfo.CurrentSeason),
					c => c.ContractID, y => y.ContractID, (c, y) => new { c.PlayerID, y.CapHit })
				.ToDictionaryAsync(x => x.PlayerID, x => (long?)x.CapHit, ct)   // nullable, so a free agent's is null, not 0
				.ConfigureAwait(false);

			var rows = players.Select(p => ToPlayerRowDto(p, perception.Perceive(saveInfo.UserTeamID, p.ToSnapshot()),
				saveInfo.CurrentDate, capHits.GetValueOrDefault(p.PlayerID)))
				.ToList();

			// 1306: the reads and the perception, for F-03 §12's 20 ms
			var elapsedMs = (long)time.GetElapsedTime(started).TotalMilliseconds;
			var viewer = saveInfo.UserTeamID is { } team ? team.Value.ToString(CultureInfo.InvariantCulture) : "none";
			PlayerLog.LeaguePlayersRead(log, rows.Count, viewer, elapsedMs);

			return rows;
		}, cancellationToken).ConfigureAwait(false);

	/// <summary>One player's page (F-03 §5), or null when there's no such player: five reads, one perception.</summary>
	public async Task<PlayerDetailDto?> GetPlayerAsync(PlayerID playerID, CancellationToken cancellationToken = default)
		=> await reader.QueryAsync(async (db, ct) =>
		{
			var saveInfo = await db.SaveInfo.SingleAsync(ct).ConfigureAwait(false);
			var player = await db.Players.FirstOrDefaultAsync(p => p.PlayerID == playerID, ct).ConfigureAwait(false);

			if (player == null)
			{
				return null;
			}

			var playerPerception = perception.Perceive(saveInfo.UserTeamID, player.ToSnapshot());

			var college = await db.Colleges.FirstAsync(c => c.CollegeID == player.CollegeID, ct).ConfigureAwait(false);

			var draft = player.IsUndrafted ? null : new DraftInfoDto(player.DraftSeason.Value, player.DraftRound.Value, player.DraftPick.Value, player.DraftTeamID.Value);

			var measurables = new Dictionary<Measurable, double>()
			{
				{ Measurable.Height, player.Height },
				{ Measurable.Weight, player.Weight },
				{ Measurable.Forty, player.Forty },
				{ Measurable.TenSplit, player.TenSplit },
				{ Measurable.ThreeCone, player.ThreeCone },
				{ Measurable.Shuttle, player.Shuttle },
				{ Measurable.Bench, player.Bench },
				{ Measurable.Vertical, player.Vertical },
				{ Measurable.BroadJump, player.BroadJump },
				{ Measurable.ArmLength, player.ArmLength },
				{ Measurable.HandSize, player.HandSize }
			};

			var contract = await db.Contracts.SingleOrDefaultAsync(c => c.PlayerID == player.PlayerID && c.IsActive, ct).ConfigureAwait(false);
			ContractSummaryDto? contractSummary = null;

			if (contract != null)
			{
				var contractYears = await db.ContractYears
					.Where(y => y.ContractID == contract.ContractID)
					.OrderBy(y => y.Season)
					.ToListAsync(ct)
					.ConfigureAwait(false);

				var left = contractYears.Where(y => y.Season >= saveInfo.CurrentSeason).ToList();

				contractSummary = new ContractSummaryDto(
					contract.Type,
					contract.SignedSeason,
					contract.Years,
					contract.TotalValue,
					contract.TotalValue / contract.Years,
					contractYears.Single(c => c.Season == saveInfo.CurrentSeason).CapHit,
					left.Count,
					left.Sum(c => c.GuaranteedAmount),
					contractYears.Select(x => new ContractYearDto(
						x.Season,
						x.BaseSalary,
						x.ProratedBonus,
						x.GuaranteedAmount,
						x.CapHit
					)).ToList()
				);
			}

			var playerRow = ToPlayerRowDto(player, playerPerception, saveInfo.CurrentDate, contractSummary?.CapHit);

			return new PlayerDetailDto(
				playerRow,
				player.BirthDate,
				college.Name,
				player.HometownName,
				player.HometownState,
				player.Experience,
				draft,
				measurables,
				playerPerception.Attributes,
				playerPerception.Personality,
				contractSummary
			);
		}, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// One row, built the same way for the roster, the league's list and the detail (WP6 §4.2). The OVR, the grade range
	/// and the archetype all come from what the viewer sees, never the true ratings.
	/// </summary>
	private PlayerRowDto ToPlayerRowDto(Player player, PerceivedPlayer perceived, DateOnly date, long? capHit)
	{
		var estimates = perceived.Attributes.ToDictionary(a => a.Key, a => a.Value.Estimate);

		return new PlayerRowDto(
			player.PlayerID,
			player.FirstName,
			player.LastName,
			player.Position,
			player.JerseyNumber,
			Player.AgeOn(player.BirthDate, date),
			player.TeamID,
			perceived.Overall,
			GradeRange(perceived.Potential),
			OverallCalculator.Archetype(_profiles[player.Position], data.Generation.Skills, estimates),
			capHit);
	}

	/// <summary>The grade of the potential estimate and of its 80% interval's ends, estimate ∓ 1.28σ (F-03 §4.9): one grade while σ is 0.</summary>
	private PotentialRange GradeRange(PerceivedRating potential)
	{
		var bands = data.Generation.Hidden.PotentialGrades;

		return new PotentialRange(
			OverallCalculator.Grade(potential.Estimate, bands),
			OverallCalculator.Grade(potential.Estimate - (1.28 * potential.Sigma), bands),
			OverallCalculator.Grade(potential.Estimate + (1.28 * potential.Sigma), bands));
	}
}
