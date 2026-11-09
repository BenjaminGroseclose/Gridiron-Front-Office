using GridironFrontOffice.Application;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Infrastructure.Tests.Leagues;

/// <summary>
/// M1 WP6 slice 2 (guide §5): LeagueQueries on a seed-42 save, checked against the team set and the generated league
/// rather than the save, so a query that reads the wrong rows can't agree with itself.
/// </summary>
[Collection(nameof(QueriedSeed42))]
public sealed class LeagueQueriesTests(QueriedSeed42 league)
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task The_structure_is_the_team_sets_conferences_divisions_and_teams_in_order()
	{
		var set = league.Data.TeamSet;

		var structure = await league.Leagues.GetStructureAsync(Ct);

		structure.Conferences.Select(c => (c.ConferenceID, c.Key, c.Name))
			.ShouldBe(set.Conferences.Select((c, i) => (new ConferenceID(i + 1), c.Key, c.Name)));
		var divisions = structure.Conferences.SelectMany(c => c.Divisions).ToList();
		divisions.Select(d => (d.DivisionID, d.Key, d.Name))
			.ShouldBe(set.Conferences.SelectMany(c => c.Divisions).Select((d, i) => (new DivisionID(i + 1), d.Key, d.Name)));
		structure.Conferences.Select(c => c.Divisions.Count).ShouldBe([4, 4]);
		divisions.Select(d => d.Teams.Count).ShouldAllBe(n => n == 4);

		// Each division's teams in the team set's order (TeamID), each identity as teams.json has it
		foreach (var (division, expected) in divisions.Zip(set.Conferences.SelectMany(c => c.Divisions)))
		{
			division.Teams.ShouldBe(set.Teams.Select((t, i) => (t, i)).Where(x => expected.Teams.Contains(x.t.Abbr)).Select(x => Identity(x.t, x.i)));
		}
	}

	[Fact]
	public async Task The_identities_are_every_team_in_TeamID_order_and_match_the_structures()
	{
		var identities = await league.Leagues.GetTeamIdentitiesAsync(Ct);
		var structure = await league.Leagues.GetStructureAsync(Ct);

		identities.ShouldBe(league.Data.TeamSet.Teams.Select(Identity));
		structure.Conferences.SelectMany(c => c.Divisions).SelectMany(d => d.Teams).OrderBy(t => t.TeamID.Value).ShouldBe(identities);
	}

	[Fact]
	public void Before_the_pick_the_header_is_the_league_at_camp_with_no_user_team() =>
		league.HeaderBeforePick.ShouldBe(new LeagueHeaderDto("Seed 42", 42, QueriedSeed42.Season, Phase.TrainingCamp, QueriedSeed42.CampDate, null));

	[Fact]
	public async Task After_the_pick_the_header_carries_the_user_teams_summary()
	{
		var header = await league.Leagues.GetHeaderAsync(Ct);

		header.ShouldBe(league.HeaderBeforePick with { UserTeam = await league.Leagues.GetTeamSummaryAsync(QueriedSeed42.UserTeam, Ct) });
		header.UserTeam.ShouldNotBeNull().Identity.TeamID.ShouldBe(QueriedSeed42.UserTeam);
	}

	[Fact]
	public async Task Each_summary_has_its_teams_names_stadium_and_cap_from_the_generated_contracts()
	{
		var summaries = await league.Leagues.GetTeamSummariesAsync(Ct);
		var set = league.Data.TeamSet;
		var contracts = league.Data.Contracts;

		summaries.Select(s => s.Identity.TeamID).ShouldBe(Enumerable.Range(1, 32).Select(i => new TeamID(i)));
		var problems = new List<string>();
		foreach (var (summary, team, index) in summaries.Zip(league.Generated.Teams).Select((x, i) => (x.First, x.Second, i)))
		{
			var definition = set.Teams[index];
			var conference = set.Conferences.Single(c => c.Divisions.Any(d => d.Teams.Contains(definition.Abbr)));
			var division = conference.Divisions.Single(d => d.Teams.Contains(definition.Abbr));
			var committed = team.Players.Sum(p => p.Contract!.Seasons.Single(s => s.Season == QueriedSeed42.Season).CapHit);

			team.Definition.ShouldBe(definition, "The generated teams are the team set's, in order.");
			var expected = (Identity(definition, index), conference.Name, division.Name, definition.Stadium.Name, definition.Stadium.Capacity,
				definition.Stadium.Roof, contracts.Cap, committed, contracts.Cap - committed);
			var actual = (summary.Identity, summary.ConferenceName, summary.DivisionName, summary.StadiumName, summary.StadiumCapacity,
				summary.StadiumRoof, summary.Cap, summary.CapCommitted, summary.CapSpace);
			if (actual != expected)
			{
				problems.Add($"{definition.Abbr}: {actual}, not {expected}");
			}

			if (committed < contracts.Utilization.Min * contracts.Cap || committed > contracts.Utilization.Max * contracts.Cap)
			{
				problems.Add($"{definition.Abbr}: ${committed:N0} committed is outside F-12 §4.3's band");
			}
		}

		problems.ShouldBeEmpty();
	}

	[Fact]
	public async Task Each_rosters_strength_is_its_starters_mean_OVR_on_each_side_and_overall()
	{
		var summaries = await league.Leagues.GetTeamSummariesAsync(Ct);
		var problems = new List<string>();

		foreach (var (summary, team) in summaries.Zip(league.Generated.Teams))
		{
			// The starters from the generated ratings, by positions.json's counts (D-092 #3)
			var starters = team.Players
				.GroupBy(p => p.Position)
				.SelectMany(g => g.Select(p => OverallCalculator.Overall(league.Profile(g.Key), p.Ratings))
					.OrderByDescending(o => o)
					.Take(league.Profile(g.Key).Roster.Starters)
					.Select(o => (Side: g.Key.Side(), Ovr: o)))
				.ToList();
			var expected = (Mean(starters.Where(s => s.Side == SideOfBall.Offense)), Mean(starters.Where(s => s.Side == SideOfBall.Defense)), Mean(starters));
			var strength = summary.RosterStrength;

			if (!Close(strength.Offense, expected.Item1) || !Close(strength.Defense, expected.Item2) || !Close(strength.Overall, expected.Item3))
			{
				problems.Add($"{team.Definition.Abbr}: {strength}, not {expected}");
			}
		}

		problems.ShouldBeEmpty();
	}

	[Fact]
	public async Task One_teams_summary_is_its_entry_in_the_list_and_the_pick_doesnt_change_any()
	{
		var summaries = await league.Leagues.GetTeamSummariesAsync(Ct);

		foreach (var summary in summaries)
		{
			(await league.Leagues.GetTeamSummaryAsync(summary.Identity.TeamID, Ct)).ShouldBe(summary);
		}

		summaries.ShouldBe(league.SummariesBeforePick, customMessage: "Every sigma is 0 in M1, so the viewer doesn't move a number.");
	}

	[Fact]
	public async Task An_unknown_team_has_no_summary() =>
		(await league.Leagues.GetTeamSummaryAsync(new TeamID(99), Ct)).ShouldBeNull();

	private static TeamIdentityDto Identity(TeamDefinition t, int index) =>
		new(new TeamID(index + 1), t.Abbr, t.City, t.Nickname, t.Colors.Primary, t.Colors.Secondary, t.Colors.Accent,
			t.Crest.Shape, t.Crest.Monogram, t.Crest.Stripe);

	private static double Mean(IEnumerable<(SideOfBall Side, double Ovr)> starters) => starters.Average(s => s.Ovr);

	// The same numbers averaged in another order can differ in the last bit
	private static bool Close(PerceivedRating actual, double expected) => Math.Abs(actual.Estimate - expected) < 1e-9 && actual.Sigma == 0;
}

/// <summary>
/// M1 WP8 (F-01 §8, §10): a save with the current schema and no league in it (made by M0's name-only New League) has
/// no teams to summarize, so the picker can say so instead of failing.
/// </summary>
public sealed class EmptySaveQueriesTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task A_save_without_a_league_has_no_summaries()
	{
		await using var save = await Data.TestSave.CreateAsync("Empty League", services => services.AddGfoApplication());
		var leagues = save.Services.GetRequiredService<LeagueQueries>();

		(await leagues.GetTeamSummariesAsync(Ct)).ShouldBeEmpty();
		(await leagues.GetTeamSummaryAsync(new TeamID(1), Ct)).ShouldBeNull();
	}
}
