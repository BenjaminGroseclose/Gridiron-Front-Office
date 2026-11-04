using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Tests.Leagues;

/// <summary>
/// M1 WP6 slice 2 (guide §5): PlayerQueries on a seed-42 save, with team 5 picked. Each row and detail is checked
/// against the generated league, and the three queries against each other.
/// </summary>
[Collection(nameof(QueriedSeed42))]
public sealed class PlayerQueriesTests(QueriedSeed42 league)
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public void The_leagues_list_is_every_player_once_in_PlayerID_order_as_generated()
	{
		var everyone = league.Everyone().ToList();

		league.League.Select(r => r.PlayerID.Value).ShouldBe(Enumerable.Range(1, everyone.Count));
		everyone.Count.ShouldBe(1846);
		var wrong = league.League.Zip(everyone, (row, x) => (row, Expected: league.Row(x.Player, x.Team)))
			.Where(x => x.row != x.Expected)
			.Select(x => $"Player {x.row.PlayerID.Value}: {x.row}, not {x.Expected}");
		wrong.Take(20).ShouldBeEmpty("The OVR, grade, archetype, age on the camp date and this season's cap hit.");
	}

	[Fact]
	public void A_free_agents_cap_hit_is_null_and_a_rostered_players_is_set()
	{
		var freeAgents = league.League.Where(r => r.TeamID is null).ToList();

		freeAgents.Count.ShouldBe(league.Generated.FreeAgents.Count);
		freeAgents.ShouldAllBe(r => r.CapHit == null, "A free agent has no contract: his cap hit is null, not $0.");
		league.League.Where(r => r.TeamID is not null).ShouldAllBe(r => r.CapHit > 0);
	}

	[Fact]
	public async Task A_roster_is_its_teams_active_players_by_position_then_OVR_then_name()
	{
		foreach (var team in Enumerable.Range(1, 32).Select(i => new TeamID(i)))
		{
			var roster = await league.Players.GetRosterAsync(team, Ct);

			roster.Count.ShouldBe(53);
			roster.ShouldBe([.. league.League.Where(r => r.TeamID == team)
				.OrderBy(r => r.Position)
				.ThenByDescending(r => r.Overall.Estimate)
				.ThenBy(r => r.LastName, StringComparer.Ordinal)
				.ThenBy(r => r.FirstName, StringComparer.Ordinal)], customMessage: $"Team {team.Value}'s roster");
		}
	}

	[Fact]
	public async Task A_players_detail_is_as_generated_and_its_row_is_his_row_in_the_leagues_list()
	{
		var bands = league.Data.Generation.Personality;
		var problems = new List<string>();

		foreach (var (p, team) in league.Everyone())
		{
			var id = new PlayerID(p.Slot + 1);
			var detail = (await league.Players.GetPlayerAsync(id, Ct)).ShouldNotBeNull();
			void Same(string what, object? actual, object? expected)
			{
				if (!Equals(actual, expected))
				{
					problems.Add($"Player {id.Value}: {what} is {actual ?? "null"}, not {expected ?? "null"}");
				}
			}

			Same("Row", detail.Row, league.League[p.Slot]);
			Same("Bio", (detail.BirthDate, detail.College, detail.HometownName, detail.HometownState, detail.Experience),
				(p.BirthDate, p.College.Name, p.Hometown.Name, p.Hometown.State, p.Experience));
			Same("Draft", detail.Draft, p.Draft is { } d ? new DraftInfoDto(d.Season, d.Round, d.Pick, league.TeamOf(d.Team)) : null);
			Same("Measurables", Text(detail.Measurables), Text(p.Measurables));
			Same("Attributes", Text(detail.Attributes), Text(p.Ratings.ToDictionary(r => r.Key, r => new PerceivedRating(r.Value, 0))));

			// D-092 #2: the user's own players only, banded by generation.json
			var personality = team == QueriedSeed42.UserTeam
				? p.Personality.ToDictionary(t => t.Key, t => t.Value < bands.LowBelow ? TraitLevel.Low : t.Value > bands.HighAbove ? TraitLevel.High : TraitLevel.Average)
				: null;
			Same("Personality", detail.Personality is null ? null : Text(detail.Personality), personality is null ? null : Text(personality));

			Same("Contract", Text(detail.Contract), p.Contract is { } deal ? Text(Expected(deal)) : null);
		}

		problems.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public void Before_the_pick_no_one_shows_a_personality_not_even_the_team_the_user_picks_later()
	{
		league.UserTeamBeforePick.Count.ShouldBe(53);
		league.UserTeamBeforePick.ShouldAllBe(d => d.Personality == null);
		league.UserTeamBeforePick.Select(d => d.Row).ShouldBe(league.LeagueBeforePick.Where(r => r.TeamID == QueriedSeed42.UserTeam));
	}

	[Fact]
	public async Task An_unknown_player_has_no_detail() =>
		(await league.Players.GetPlayerAsync(new PlayerID(99_999), Ct)).ShouldBeNull();

	[Fact]
	public void Each_read_of_the_leagues_list_logs_1306_once_with_its_count_viewer_and_time()
	{
		var logged = league.PlayerLog.Events.Where(e => e.EventID == 1306).ToList();

		logged.Select(e => (e.Level, e.Values["Players"], e.Values["Viewer"])).ShouldBe(
			new (LogLevel, object?, object?)[] { (LogLevel.Debug, 1846, "none"), (LogLevel.Debug, 1846, "5") },
			customMessage: "Once before the pick, once after.");
		foreach (var (e, wall) in logged.Zip(league.LeagueReadMs))
		{
			((long)e.Values["ElapsedMs"]!).ShouldBeInRange(0, wall, "The time covers the reads and the perception, and no more than the call.");
		}

		league.PlayerLog.Events.ShouldAllBe(e => e.EventID == 1306, "The roster and the detail don't log (WP6 §4.4).");
	}

	/// <summary>F-12 §8's panel for a generated deal: this season's cap hit, and the years and guarantees from this season on.</summary>
	private static ContractSummaryDto Expected(GeneratedContract deal)
	{
		var left = deal.Seasons.Where(s => s.Season >= QueriedSeed42.Season).ToList();
		return new ContractSummaryDto(deal.Type, deal.SignedSeason, deal.Seasons.Count, deal.TotalValue, deal.TotalValue / deal.Seasons.Count,
			deal.Seasons.Single(s => s.Season == QueriedSeed42.Season).CapHit, left.Count, left.Sum(s => s.GuaranteedAmount),
			[.. deal.Seasons.Select(s => new ContractYearDto(s.Season, s.BaseSalary, s.ProratedBonus, s.GuaranteedAmount, s.CapHit))]);
	}

	/// <summary>A dictionary as text in key order, so two compare by content.</summary>
	private static string Text<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> values)
		where TKey : notnull =>
		string.Join("; ", values.OrderBy(v => v.Key).Select(v => $"{v.Key}={v.Value}"));

	/// <summary>A contract summary as text with its seasons spelled out (the list compares by reference).</summary>
	private static string? Text(ContractSummaryDto? c) =>
		c is null ? null : $"{c with { Seasons = [] }} | {string.Join("; ", c.Seasons)}";
}
