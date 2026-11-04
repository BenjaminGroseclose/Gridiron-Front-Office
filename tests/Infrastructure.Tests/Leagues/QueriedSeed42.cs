using System.Diagnostics;
using GridironFrontOffice.Application;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Leagues;

/// <summary>The query tests share one seed-42 save (WP6 §5), so they run one at a time, in this collection.</summary>
[CollectionDefinition(nameof(QueriedSeed42))]
public sealed class QueriedSeed42Readers : ICollectionFixture<QueriedSeed42>;

/// <summary>
/// The seed-42 league the query tests read (M1 WP6 §5): created through CreateLeague, with the query services resolved
/// from the hosts' AddGfoApplication. It reads once before the user's pick, then picks team 5. Every
/// GetLeaguePlayersAsync call is made here, so the tests know exactly which 1306s were logged.
/// </summary>
public sealed class QueriedSeed42 : IAsyncLifetime
{
	internal const int Season = 2026;

	internal static readonly TeamID UserTeam = new(5);

	internal TestSave Save { get; private set; } = null!;

	internal ISeedData Data { get; private set; } = null!;

	internal GeneratedLeague Generated { get; private set; } = null!;

	internal LeagueQueries Leagues => Save.Services.GetRequiredService<LeagueQueries>();

	internal PlayerQueries Players => Save.Services.GetRequiredService<PlayerQueries>();

	internal CapturingLogger<PlayerQueries> PlayerLog { get; } = new();

	internal LeagueHeaderDto HeaderBeforePick { get; private set; } = null!;

	internal IReadOnlyList<TeamSummaryDto> SummariesBeforePick { get; private set; } = null!;

	internal IReadOnlyList<PlayerRowDto> LeagueBeforePick { get; private set; } = null!;

	/// <summary>Team 5's players' details before the pick, when the user is an outsider.</summary>
	internal IReadOnlyList<PlayerDetailDto> UserTeamBeforePick { get; private set; } = null!;

	internal IReadOnlyList<PlayerRowDto> League { get; private set; } = null!;

	/// <summary>How long each GetLeaguePlayersAsync call took, measured around it: its 1306 can't report more.</summary>
	internal List<long> LeagueReadMs { get; } = [];

	internal static DateOnly CampDate => LeagueCalendar.TrainingCampOpens(Season);

	public async ValueTask InitializeAsync()
	{
		var ct = TestContext.Current.CancellationToken;
		Save = await TestSave.CreateAsync(configure: services => services
			.AddGfoApplication()
			.AddSingleton<ILogger<PlayerQueries>>(PlayerLog));
		Data = Save.Services.GetRequiredService<ISeedData>();

		var created = await CreateLeagueTests.NewCreateLeague(Save, Data, Data)
			.RunAsync(new NewLeague("Seed 42", 42, Season), cancellationToken: ct);
		created.IsSuccess.ShouldBeTrue(created.ToString());
		Generated = new LeagueGenerator(Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System).Generate(42, Season);

		HeaderBeforePick = await Leagues.GetHeaderAsync(ct);
		SummariesBeforePick = await Leagues.GetTeamSummariesAsync(ct);
		LeagueBeforePick = await ReadLeagueAsync(ct);
		var userTeam = Everyone().Where(x => x.Team == UserTeam).Select(x => new PlayerID(x.Player.Slot + 1)).ToList();
		var details = new List<PlayerDetailDto>();
		foreach (var player in userTeam)
		{
			details.Add((await Players.GetPlayerAsync(player, ct)).ShouldNotBeNull());
		}

		UserTeamBeforePick = details;

		var picked = await new LeagueCommands(Save.Writer, NullLogger<LeagueCommands>.Instance).PickUserTeamAsync(UserTeam, ct);
		picked.IsSuccess.ShouldBeTrue(picked.ToString());
		League = await ReadLeagueAsync(ct);
	}

	public ValueTask DisposeAsync() => Save.DisposeAsync();

	/// <summary>Every generated player in slot order (PlayerID = slot + 1), with the ID of his team (null for a free agent).</summary>
	internal IEnumerable<(GeneratedPlayer Player, TeamID? Team)> Everyone() =>
		Generated.Teams
			.SelectMany((t, i) => t.Players.Select(p => (p, (TeamID?)new TeamID(i + 1))))
			.Concat(Generated.FreeAgents.Select(p => (p, (TeamID?)null)))
			.OrderBy(x => x.p.Slot);

	internal TeamID TeamOf(TeamDefinition team) =>
		new(Data.TeamSet.Teams.Select((t, i) => (t.Abbr, i)).Single(x => x.Abbr == team.Abbr).i + 1);

	/// <summary>The row a screen should get for <paramref name="p"/> on <paramref name="team"/>, built from the generated league, not the save.</summary>
	internal PlayerRowDto Row(GeneratedPlayer p, TeamID? team)
	{
		var profile = Profile(p.Position);
		var grade = OverallCalculator.Grade(p.Hidden.Potential, Data.Generation.Hidden.PotentialGrades);
		return new PlayerRowDto(new PlayerID(p.Slot + 1), p.FirstName, p.LastName, p.Position, p.JerseyNumber,
			Player.AgeOn(p.BirthDate, CampDate), team, new PerceivedRating(OverallCalculator.Overall(profile, p.Ratings), 0),
			new PotentialRange(grade, grade, grade), OverallCalculator.Archetype(profile, Data.Generation.Skills, p.Ratings),
			p.Contract?.Seasons.Single(s => s.Season == Season).CapHit);
	}

	internal PositionProfile Profile(Position position) => Data.Positions.Positions.Single(p => p.Position == position);

	private async Task<IReadOnlyList<PlayerRowDto>> ReadLeagueAsync(CancellationToken ct)
	{
		var clock = Stopwatch.StartNew();
		var rows = await Players.GetLeaguePlayersAsync(ct);
		LeagueReadMs.Add(clock.ElapsedMilliseconds);
		return rows;
	}
}
