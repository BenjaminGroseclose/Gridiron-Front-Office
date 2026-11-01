using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Leagues;

/// <summary>
/// M1 WP5 slice 3 (guide §5): the user's team, picked through a real save. Each test writes the seed-42 league,
/// generated once, to a save of its own, so it starts with no team picked and its own <c>created</c> snapshot.
/// </summary>
public sealed class LeagueCommandsTests(GeneratedSeed42 league) : IClassFixture<GeneratedSeed42>
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task A_pick_saves_the_team_raises_UserTeamPicked_after_the_commit_and_logs_1305_last()
	{
		await using var save = await TestSave.CreateAsync();
		var created = await league.WriteToAsync(save);
		var log = new CapturingLogger<LeagueCommands>();
		var heard = new List<(UserTeamPicked Event, string? Committed, int Logged)>();
		using var subscription = save.Events.Subscribe<UserTeamPicked>(e =>
			heard.Add((e, TestSave.ScalarText(created.FilePath, "SELECT quote(UserTeamID) FROM SaveInfo"), log.Events.Count)));

		var picked = await new LeagueCommands(save.Writer, log).PickUserTeamAsync(new TeamID(5), Ct);

		picked.IsSuccess.ShouldBeTrue(picked.ToString());
		(await UserTeamAsync(save)).ShouldBe(new TeamID(5));
		heard.ShouldBe([(new UserTeamPicked(new TeamID(5)), "5", 0)], "The event comes after the commit, and 1305 after the event.");
		var logged = log.Events.ShouldHaveSingleItem();
		(logged.EventID, logged.Level, logged.Values["Abbreviation"]).ShouldBe((1305, LogLevel.Information, (object?)Abbreviation(5)));
	}

	[Fact]
	public async Task An_unknown_team_is_refused_with_nothing_changed_no_event_and_no_1305()
	{
		await using var save = await TestSave.CreateAsync();
		var created = await league.WriteToAsync(save);
		var log = new CapturingLogger<LeagueCommands>();
		var commands = new LeagueCommands(save.Writer, log);
		(await commands.PickUserTeamAsync(new TeamID(5), Ct)).IsSuccess.ShouldBeTrue();
		var before = CreateLeagueTests.Dump(created.FilePath, "SaveInfo");
		var heard = new List<UserTeamPicked>();
		using var subscription = save.Events.Subscribe<UserTeamPicked>(heard.Add);

		var refused = await commands.PickUserTeamAsync(new TeamID(league.Data.TeamSet.Teams.Count + 1), Ct);

		refused.Error.ShouldNotBeNull().Code.ShouldBe("UnknownTeam");
		CreateLeagueTests.Dump(created.FilePath, "SaveInfo").ShouldBe(before, "The first pick stands, and not even the generation moves.");
		heard.ShouldBeEmpty();
		log.Events.Select(e => e.EventID).ShouldBe([1305], "Only the first pick is logged.");
	}

	[Fact]
	public async Task A_second_pick_replaces_the_first()
	{
		await using var save = await TestSave.CreateAsync();
		await league.WriteToAsync(save);
		var log = new CapturingLogger<LeagueCommands>();
		var commands = new LeagueCommands(save.Writer, log);
		var heard = new List<UserTeamPicked>();
		using var subscription = save.Events.Subscribe<UserTeamPicked>(heard.Add);
		(await commands.PickUserTeamAsync(new TeamID(5), Ct)).IsSuccess.ShouldBeTrue();

		var second = await commands.PickUserTeamAsync(new TeamID(12), Ct);

		second.IsSuccess.ShouldBeTrue(second.ToString());
		(await UserTeamAsync(save)).ShouldBe(new TeamID(12));
		heard.ShouldBe([new UserTeamPicked(new TeamID(5)), new UserTeamPicked(new TeamID(12))]);
		log.Events.Select(e => e.Values["Abbreviation"]).ShouldBe([Abbreviation(5), Abbreviation(12)]);
	}

	[Fact]
	public async Task Restoring_the_created_snapshot_brings_back_no_team()
	{
		await using var save = await TestSave.CreateAsync();
		var created = await league.WriteToAsync(save);
		(await new LeagueCommands(save.Writer, NullLogger<LeagueCommands>.Instance).PickUserTeamAsync(new TeamID(5), Ct))
			.IsSuccess.ShouldBeTrue();
		var snapshot = (await save.Snapshots.ListAsync(created.SaveID, Ct)).Single(s => s.Kind == SnapshotKind.Created);

		var restored = await save.Saves.RestoreAsync(snapshot, Ct);

		restored.IsSuccess.ShouldBeTrue(restored.ToString());
		(await UserTeamAsync(save)).ShouldBeNull("D-078 #7: the pick lives only in SaveInfo, so it goes back with the save.");
	}

	private static Task<TeamID?> UserTeamAsync(TestSave save) =>
		save.Reader.QueryAsync((db, ct) => db.SaveInfo.Select(i => i.UserTeamID).SingleAsync(ct), Ct);

	/// <summary>A team's abbreviation by its ID: LeagueWriter numbers the teams in the team set's order.</summary>
	private string Abbreviation(int team) => league.Data.TeamSet.Teams[team - 1].Abbr;
}

/// <summary>The seed-42 league, generated once; each test writes it to a save of its own, as CreateLeague does.</summary>
public sealed class GeneratedSeed42 : IAsyncLifetime
{
	private TestSave _save = null!;

	internal ISeedData Data => _save.Services.GetRequiredService<ISeedData>();

	internal GeneratedLeague Generated { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		_save = await TestSave.CreateAsync();
		Generated = new LeagueGenerator(Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System).Generate(42, 2026);
	}

	/// <summary>Creates the league's save, with its <c>created</c> snapshot, next to <paramref name="save"/>'s and opens it.</summary>
	internal async Task<OpenSave> WriteToAsync(TestSave save)
	{
		var data = save.Services.GetRequiredService<ISeedData>();
		var created = await save.Saves.CreateAsync("Seed 42",
			(uow, ct) => LeagueWriter.WriteAsync(uow, Generated, data, ct), TestContext.Current.CancellationToken);
		created.IsSuccess.ShouldBeTrue(created.ToString());
		return created.Value;
	}

	public ValueTask DisposeAsync() => _save.DisposeAsync();
}
