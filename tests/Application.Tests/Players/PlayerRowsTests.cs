using System.Reflection;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Tests.Leagues;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// D-091: <see cref="PlayerRows.ToPlayer"/>, the one conversion from a generated player to his row. It sets 59 ratings
/// by name, one line each, so a crossed pair or a forgotten line is caught here rather than on a ratings screen.
/// </summary>
public sealed class PlayerRowsTests
{
	private static readonly GeneratedLeague League = GeneratedLeagues.Twenty[0].League;

	[Fact]
	public void Every_rating_lands_in_the_property_named_after_it()
	{
		var problems = new List<string>();

		foreach (var p in League.Teams.SelectMany(t => t.Players).Concat(League.FreeAgents))
		{
			var row = p.ToPlayer(new PlayerID(1), null, PlayerStatus.FreeAgent, new CollegeID(1), null);
			Check(p, row, Enum.GetValues<Measurable>().Select(m => (m.ToString(), (object)p.Measurables[m])), problems);
			Check(p, row, Enum.GetValues<PlayerAttribute>().Select(a => (a.ToString(), (object)p.Ratings[a])), problems);
			Check(p, row, Enum.GetValues<PersonalityTrait>().Select(t => (t.ToString(), (object)p.Personality[t])), problems);
			Check(p, row, typeof(HiddenRatings).GetProperties().Select(h => (h.Name, h.GetValue(p.Hidden)!)), problems);
		}

		problems.ShouldBeEmpty();
	}

	[Fact]
	public void The_IDs_and_the_status_are_the_callers_and_the_bio_and_draft_are_the_players()
	{
		var p = League.Teams.SelectMany(t => t.Players).First(x => x.Draft is not null);

		var row = p.ToPlayer(new PlayerID(812), new TeamID(7), PlayerStatus.Active, new CollegeID(44), new TeamID(19));

		(row.PlayerID, row.TeamID, row.Status, row.CollegeID, row.DraftTeamID)
			.ShouldBe((new PlayerID(812), (TeamID?)new TeamID(7), PlayerStatus.Active, new CollegeID(44), (TeamID?)new TeamID(19)));
		(row.Position, row.FirstName, row.LastName, row.BirthDate, row.Experience, row.JerseyNumber)
			.ShouldBe((p.Position, p.FirstName, p.LastName, p.BirthDate, p.Experience, p.JerseyNumber));
		(row.HometownName, row.HometownState).ShouldBe((p.Hometown.Name, p.Hometown.State));
		(row.DraftSeason, row.DraftRound, row.DraftPick).ShouldBe(((int?)p.Draft!.Season, (int?)p.Draft.Round, (int?)p.Draft.Pick));
	}

	[Fact]
	public void An_undrafted_free_agent_has_no_team_draft_or_jersey()
	{
		var p = League.FreeAgents.First(x => x.Draft is null);

		var row = p.ToPlayer(new PlayerID(1800), null, PlayerStatus.FreeAgent, new CollegeID(3), null);

		(row.TeamID, row.DraftTeamID, row.DraftSeason, row.DraftRound, row.DraftPick, row.JerseyNumber)
			.ShouldBe((null, null, null, null, null, null));
	}

	/// <summary>Each (property, expected value) pair against the row's property of that name, which must exist.</summary>
	private static void Check(GeneratedPlayer p, Player row, IEnumerable<(string Name, object Expected)> values, List<string> problems)
	{
		foreach (var (name, expected) in values)
		{
			var property = typeof(Player).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			var actual = property?.GetValue(row);
			if (!Equals(actual, expected))
			{
				problems.Add($"Slot {p.Slot}: {name} is {actual ?? "missing"}, not {expected}");
			}
		}
	}
}
