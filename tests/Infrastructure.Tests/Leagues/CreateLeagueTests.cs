using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Infrastructure.Tests.Leagues;

/// <summary>
/// M1 WP5 slice 2 (guide §5): New League through real saves. One seed-42 league, created once, serves the read-only
/// checks: the round trip, the IDs and counts, the snapshot, the progress and 1304. The rest make their own.
/// </summary>
public sealed class CreateLeagueTests(Seed42League league) : IClassFixture<Seed42League>
{
	private const ulong Seed = 42;
	private const int Season = 2026;

	private static readonly string[] LeagueTables = ["Seasons", "Conferences", "Divisions", "Teams", "Colleges", "Players", "Contracts", "ContractYears"];

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task The_conferences_divisions_teams_and_colleges_are_the_data_files_in_order()
	{
		var set = league.Data.TeamSet;
		var (conferences, divisions, teams, colleges) = await league.Save.Reader.QueryAsync(async (db, ct) => (
			await db.Conferences.ToListAsync(ct), await db.Divisions.ToListAsync(ct), await db.Teams.ToListAsync(ct), await db.Colleges.ToListAsync(ct)), Ct);

		conferences.OrderBy(c => c.ConferenceID.Value).Select(c => (c.ConferenceID, c.Key, c.Name))
			.ShouldBe(set.Conferences.Select((c, i) => (new ConferenceID(i + 1), c.Key, c.Name)));

		// Numbered conference by conference, so a division's ID follows from its place in the team set
		var expectedDivisions = set.Conferences
			.SelectMany((c, ci) => c.Divisions.Select(d => (Conference: new ConferenceID(ci + 1), Division: d)))
			.Select((d, i) => (ID: new DivisionID(i + 1), d.Conference, d.Division))
			.ToList();
		divisions.OrderBy(d => d.DivisionID.Value).Select(d => (d.DivisionID, d.ConferenceID, d.Key, d.Name))
			.ShouldBe(expectedDivisions.Select(d => (d.ID, d.Conference, d.Division.Key, d.Division.Name)));

		var divisionOf = expectedDivisions.SelectMany(d => d.Division.Teams.Select(abbr => (abbr, d.ID))).ToDictionary(x => x.abbr, x => x.ID);
		teams.OrderBy(t => t.TeamID.Value).Select(t => new
		{
			t.TeamID, t.DivisionID, t.Abbreviation, t.City, t.Nickname, t.PrimaryColor, t.SecondaryColor, t.AccentColor,
			t.CrestShape, t.CrestMonogram, t.CrestStripe, t.StadiumName, t.StadiumCapacity, t.StadiumRoof, t.StadiumSurface, t.StadiumClimate,
		}).ShouldBe(set.Teams.Select((t, i) => new
		{
			TeamID = new TeamID(i + 1), DivisionID = divisionOf[t.Abbr], Abbreviation = t.Abbr, t.City, t.Nickname,
			PrimaryColor = t.Colors.Primary, SecondaryColor = t.Colors.Secondary, AccentColor = t.Colors.Accent,
			CrestShape = t.Crest.Shape, CrestMonogram = t.Crest.Monogram, CrestStripe = t.Crest.Stripe, StadiumName = t.Stadium.Name,
			StadiumCapacity = t.Stadium.Capacity, StadiumRoof = t.Stadium.Roof, StadiumSurface = t.Stadium.Surface, StadiumClimate = t.Stadium.Climate,
		}));

		colleges.OrderBy(c => c.CollegeID.Value).Select(c => (c.CollegeID, c.Name, c.Region, c.Tier))
			.ShouldBe(league.Data.Colleges.Colleges.Select((c, i) => (new CollegeID(i + 1), c.Name, c.Region, c.Tier)));
	}

	[Fact]
	public async Task Every_player_column_reads_back_as_generated()
	{
		var rows = (await league.Save.Reader.QueryAsync((db, ct) => db.Players.ToListAsync(ct), Ct)).ToDictionary(p => p.PlayerID);
		var problems = new List<string>();

		foreach (var (p, team) in Everyone(league.Generated, league.Data))
		{
			if (!rows.TryGetValue(new PlayerID(p.Slot + 1), out var row))
			{
				problems.Add($"Slot {p.Slot}: no player {p.Slot + 1}");
				continue;
			}

			Same(problems, p, "TeamID", row.TeamID, team);
			Same(problems, p, "Status", row.Status, team is null ? PlayerStatus.FreeAgent : PlayerStatus.Active);
			Same(problems, p, "Bio", (row.Position, row.FirstName, row.LastName, row.BirthDate, row.Experience, row.JerseyNumber),
				(p.Position, p.FirstName, p.LastName, p.BirthDate, p.Experience, p.JerseyNumber));
			Same(problems, p, "College and hometown", (row.CollegeID, row.HometownName, row.HometownState),
				(CollegeIDs(league.Data)[p.College.Name], p.Hometown.Name, p.Hometown.State));
			Same(problems, p, "Draft", (row.DraftSeason, row.DraftRound, row.DraftPick, row.DraftTeamID),
				(p.Draft?.Season, p.Draft?.Round, p.Draft?.Pick, p.Draft is { } d ? TeamIDs(league.Data)[d.Team.Abbr] : (TeamID?)null));

			// Doubles exactly: SQLite's REAL is the same 8 bytes
			foreach (var (name, expected) in Ratings(p))
			{
				Same(problems, p, name, typeof(Player).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row), expected);
			}
		}

		rows.Count.ShouldBe(Everyone(league.Generated, league.Data).Count());
		problems.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public async Task Every_contract_and_contract_year_reads_back_as_generated()
	{
		var (contracts, years) = await league.Save.Reader.QueryAsync(async (db, ct) => (await db.Contracts.ToListAsync(ct), await db.ContractYears.ToListAsync(ct)), Ct);
		var byPlayer = contracts.ToDictionary(c => c.PlayerID);
		var yearsOf = years.ToLookup(y => y.ContractID);
		var problems = new List<string>();

		foreach (var (p, team) in Everyone(league.Generated, league.Data))
		{
			var found = byPlayer.TryGetValue(new PlayerID(p.Slot + 1), out var row);
			if (p.Contract is not { } deal)
			{
				Same(problems, p, "A free agent's contract", found, false);
				continue;
			}

			if (!found)
			{
				problems.Add($"Slot {p.Slot}: no contract");
				continue;
			}

			Same(problems, p, "Contract", (row!.TeamID, row.Type, row.SignedSeason, row.Years, row.TotalValue, row.SigningBonus, row.GuaranteedTotal, row.IsActive),
				(team!.Value, deal.Type, deal.SignedSeason, deal.Seasons.Count, deal.TotalValue, deal.SigningBonus, deal.GuaranteedTotal, true));
			Same(problems, p, "Years",
				string.Join("; ", yearsOf[row.ContractID].OrderBy(y => y.Season).Select(y => (y.Season, y.BaseSalary, y.ProratedBonus, y.GuaranteedAmount, y.CapHit))),
				string.Join("; ", deal.Seasons.Select(s => (s.Season, s.BaseSalary, s.ProratedBonus, s.GuaranteedAmount, s.CapHit))));
		}

		contracts.Count.ShouldBe(byPlayer.Count, "One contract per player at most.");
		problems.Take(20).ShouldBeEmpty();
	}

	[Fact]
	public async Task The_cap_and_the_league_position_are_saved_and_no_team_is_picked()
	{
		var (seasons, info) = await league.Save.Reader.QueryAsync(async (db, ct) => (await db.Seasons.ToListAsync(ct), await db.SaveInfo.SingleAsync(ct)), Ct);

		seasons.Select(s => (s.Season, s.Cap)).ShouldBe([(Season, league.Data.Contracts.Cap)]);
		(info.Seed, info.CurrentSeason, info.CurrentPhase, info.CurrentDate, info.UserTeamID)
			.ShouldBe((Seed, Season, Phase.TrainingCamp, new DateOnly(2026, 7, 28), (TeamID?)null), "The fourth Tuesday of July 2026 is the 28th.");
	}

	[Fact]
	public async Task The_IDs_and_counts_follow_the_guides_rules_and_every_cap_hit_is_CapMaths()
	{
		var path = league.Created.FilePath;
		var set = league.Data.TeamSet;
		var everyone = Everyone(league.Generated, league.Data).ToList();
		var deals = everyone.Where(x => x.Player.Contract is not null).Select(x => x.Player.Contract!).ToList();

		var counts = LeagueTables.Skip(1).Select(table => Count(path, $"SELECT COUNT(*) FROM \"{table}\""));
		counts.ShouldBe([set.Conferences.Count, set.Conferences.Sum(c => c.Divisions.Count), set.Teams.Count, league.Data.Colleges.Colleges.Count,
			everyone.Count, deals.Count, deals.Sum(d => d.Seasons.Count)]);

		// PlayerID = slot + 1 from 1 with no gaps, and contracts numbered from 1 in PlayerID order
		(Count(path, "SELECT MIN(PlayerID) FROM Players"), Count(path, "SELECT MAX(PlayerID) FROM Players")).ShouldBe((1, everyone.Count));
		(Count(path, "SELECT MIN(ContractID) FROM Contracts"), Count(path, "SELECT MAX(ContractID) FROM Contracts")).ShouldBe((1, deals.Count));
		Count(path, "SELECT COUNT(*) FROM Contracts a JOIN Contracts b ON a.ContractID < b.ContractID AND a.PlayerID > b.PlayerID").ShouldBe(0);
		Count(path, "SELECT COUNT(*) FROM pragma_foreign_key_check").ShouldBe(0);

		var (contracts, years) = await league.Save.Reader.QueryAsync(async (db, ct) => (await db.Contracts.ToListAsync(ct), await db.ContractYears.ToListAsync(ct)), Ct);
		var yearsOf = years.ToLookup(y => y.ContractID);
		var wrong = contracts
			.SelectMany(c => yearsOf[c.ContractID].OrderBy(y => y.Season).Select((y, i) => (c, y, i)))
			.Where(x => x.y.CapHit != CapMath.CapHit(x.y.BaseSalary, x.c.SigningBonus, x.c.Years, x.i))
			.Select(x => $"Contract {x.c.ContractID.Value}, {x.y.Season}: {x.y.CapHit}");
		wrong.ShouldBeEmpty("F-12 §5: a season's cap hit is its base plus its share of the bonus.");
	}

	[Fact]
	public async Task The_created_snapshot_holds_the_league()
	{
		var snapshot = (await league.Save.Snapshots.ListAsync(league.Created.SaveID, Ct)).ShouldHaveSingleItem();

		snapshot.Kind.ShouldBe(SnapshotKind.Created);
		var everyone = Everyone(league.Generated, league.Data).ToList();
		(Count(snapshot.FilePath, "SELECT COUNT(*) FROM Players"), Count(snapshot.FilePath, "SELECT COUNT(*) FROM Contracts"))
			.ShouldBe((everyone.Count, everyone.Count(x => x.Player.Contract is not null)));
		TestSave.ScalarText(snapshot.FilePath, "SELECT CurrentPhase FROM SaveInfo").ShouldBe("TrainingCamp");
	}

	[Fact]
	public void Progress_reports_generating_then_saving() =>
		league.Progress.ShouldBe([LeagueCreationStep.Generating, LeagueCreationStep.Saving]);

	[Fact]
	public void A_created_league_logs_1304_with_its_counts_seed_season_and_time()
	{
		var logged = league.Log.Events.Where(e => e.EventID == 1304).ShouldHaveSingleItem();
		logged.Level.ShouldBe(LogLevel.Information);

		// Every rostered player has a contract and no free agent does, so the counts follow from the data
		var teams = league.Data.TeamSet.Teams.Count;
		var roster = league.Data.Generation.Roster;
		(logged.Values["LeagueName"], logged.Values["Teams"], logged.Values["Players"], logged.Values["Contracts"])
			.ShouldBe(("Seed 42", teams, teams * roster.Active + roster.FreeAgents, teams * roster.Active));
		(logged.Values["Seed"], logged.Values["Season"]).ShouldBe((Seed, Season));
		((long)logged.Values["ElapsedMs"]!).ShouldBeInRange(1, league.WallMs, "The time covers generation and the save, and no more than the call.");
	}

	[Fact]
	public async Task Rows_added_later_continue_past_the_written_IDs()
	{
		await using var save = await TestSave.CreateAsync();
		var data = save.Services.GetRequiredService<ISeedData>();
		var created = await NewCreateLeague(save, data, data).RunAsync(new NewLeague("Later Rows", Seed, Season), cancellationToken: Ct);
		created.IsSuccess.ShouldBeTrue(created.ToString());
		var path = created.Value.FilePath;
		var (players, contracts) = (Count(path, "SELECT COUNT(*) FROM Players"), Count(path, "SELECT COUNT(*) FROM Contracts"));

		// M5's prospects and M4's contracts arrive through EF, with generated keys
		var added = await save.Writer.ExecuteAsync("Test.AddLater", async (uow, ct) =>
		{
			var prospect = new Player
			{
				Status = PlayerStatus.Prospect, Position = Position.QB, FirstName = "Later", LastName = "Prospect",
				BirthDate = new DateOnly(2004, 5, 1), CollegeID = new CollegeID(1), HometownName = "Dayton", HometownState = "OH",
			};
			uow.Db.Players.Add(prospect);
			await uow.Db.SaveChangesAsync(ct);
			var deal = new Contract
			{
				PlayerID = prospect.PlayerID, TeamID = new TeamID(1), Type = ContractType.Rookie, SignedSeason = Season, Years = 4,
				TotalValue = 4_000_000, SigningBonus = 400_000, GuaranteedTotal = 400_000, IsActive = true,
			};
			uow.Db.Contracts.Add(deal);
			return Result.Ok((prospect.PlayerID, Deal: deal));
		}, Ct);

		added.IsSuccess.ShouldBeTrue(added.ToString());
		(added.Value.PlayerID, added.Value.Deal.ContractID).ShouldBe((new PlayerID((int)players + 1), new ContractID((int)contracts + 1)));
	}

	[Fact]
	public async Task A_write_that_fails_partway_leaves_no_save_behind_and_the_exception_surfaces()
	{
		await using var save = await TestSave.CreateAsync();
		await save.Saves.CloseAsync(Ct);
		var before = Files(save);
		var data = save.Services.GetRequiredService<ISeedData>();
		var log = new CapturingLogger<CreateLeague>();

		// The writer's college list keeps only the first college, so the players' insert stops at the first player from
		// any other, after the structure, the colleges and some players are in
		var oneCollege = new SeedDataWith(data, colleges: data.Colleges with { Colleges = [data.Colleges.Colleges[0]] });

		await Should.ThrowAsync<KeyNotFoundException>(() =>
			NewCreateLeague(save, data, oneCollege, log).RunAsync(new NewLeague("Half Made", Seed, Season), cancellationToken: Ct));

		save.Saves.Current.ShouldBeNull();
		Files(save).ShouldBe(before, "No .gfo, -wal, -shm or snapshot folder is left.");
		log.Events.ShouldNotContain(e => e.EventID == 1304);
	}

	[Fact]
	public async Task A_cancel_while_saving_leaves_no_save_behind()
	{
		await using var save = await TestSave.CreateAsync();
		await save.Saves.CloseAsync(Ct);
		var before = Files(save);
		var data = save.Services.GetRequiredService<ISeedData>();
		using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);

		// The writer reads the cap first, inside the command: the save's file exists by then
		var cancelledInside = false;
		var cancelling = new SeedDataWith(data, onContracts: () =>
		{
			cancelledInside = true;
			cancel.Cancel();
		});

		await Should.ThrowAsync<OperationCanceledException>(() =>
			NewCreateLeague(save, data, cancelling).RunAsync(new NewLeague("Cancelled", Seed, Season), cancellationToken: cancel.Token));

		cancelledInside.ShouldBeTrue("The cancel came from inside the write step.");
		save.Saves.Current.ShouldBeNull();
		Files(save).ShouldBe(before, "No .gfo, -wal, -shm or snapshot folder is left.");
	}

	[Fact]
	public async Task The_same_seed_and_season_make_the_same_rows()
	{
		await using var save = await TestSave.CreateAsync();
		var data = save.Services.GetRequiredService<ISeedData>();

		var first = await NewCreateLeague(save, data, data).RunAsync(new NewLeague("First", Seed, Season), cancellationToken: Ct);
		var second = await NewCreateLeague(save, data, data).RunAsync(new NewLeague("Second", Seed, Season), cancellationToken: Ct);

		first.IsSuccess.ShouldBeTrue(first.ToString());
		second.IsSuccess.ShouldBeTrue(second.ToString());
		foreach (var table in LeagueTables)
		{
			Dump(second.Value.FilePath, table).ShouldBe(Dump(first.Value.FilePath, table), $"{table} differs.");
		}

		const string position = "SELECT Seed || ' ' || CurrentSeason || ' ' || CurrentPhase || ' ' || CurrentDate FROM SaveInfo";
		TestSave.ScalarText(second.Value.FilePath, position).ShouldBe(TestSave.ScalarText(first.Value.FilePath, position));
	}

	[Fact]
	public async Task A_blank_name_is_refused_with_no_file_and_no_1304()
	{
		await using var save = await TestSave.CreateAsync();
		await save.Saves.CloseAsync(Ct);
		var before = Files(save);
		var data = save.Services.GetRequiredService<ISeedData>();
		var log = new CapturingLogger<CreateLeague>();

		var refused = await NewCreateLeague(save, data, data, log).RunAsync(new NewLeague("   ", Seed, Season), cancellationToken: Ct);

		refused.Error.ShouldNotBeNull().Code.ShouldBe(SaveErrors.InvalidName("").Code);
		Files(save).ShouldBe(before);
		log.Events.ShouldNotContain(e => e.EventID == 1304);
	}

	internal static CreateLeague NewCreateLeague(TestSave save, ISeedData generatorData, ISeedData writerData, ILogger<CreateLeague>? log = null) =>
		new(save.Saves, new LeagueGenerator(generatorData, NullLogger<LeagueGenerator>.Instance, TimeProvider.System), writerData,
			TimeProvider.System, log ?? NullLogger<CreateLeague>.Instance);

	/// <summary>Every generated player in slot order, with the ID of the team whose roster he's on (null for a free agent).</summary>
	private static IEnumerable<(GeneratedPlayer Player, TeamID? Team)> Everyone(GeneratedLeague generated, ISeedData data) =>
		generated.Teams
			.SelectMany(t => t.Players.Select(p => (p, (TeamID?)TeamIDs(data)[t.Definition.Abbr])))
			.Concat(generated.FreeAgents.Select(p => (p, (TeamID?)null)))
			.OrderBy(x => x.p.Slot);

	private static Dictionary<string, TeamID> TeamIDs(ISeedData data) =>
		data.TeamSet.Teams.Select((t, i) => (t.Abbr, ID: new TeamID(i + 1))).ToDictionary(x => x.Abbr, x => x.ID);

	private static Dictionary<string, CollegeID> CollegeIDs(ISeedData data) =>
		data.Colleges.Colleges.Select((c, i) => (c.Name, ID: new CollegeID(i + 1))).ToDictionary(x => x.Name, x => x.ID);

	/// <summary>Every rating a player row holds, by the property's name: the enums' members and the hidden ratings.</summary>
	private static IEnumerable<(string Name, object Value)> Ratings(GeneratedPlayer p) =>
		Enum.GetValues<Measurable>().Select(m => (m.ToString(), (object)p.Measurables[m]))
			.Concat(Enum.GetValues<PlayerAttribute>().Select(a => (a.ToString(), (object)p.Ratings[a])))
			.Concat(Enum.GetValues<PersonalityTrait>().Select(t => (t.ToString(), (object)p.Personality[t])))
			.Concat(typeof(HiddenRatings).GetProperties().Select(h => (h.Name, h.GetValue(p.Hidden)!)));

	private static void Same(List<string> problems, GeneratedPlayer p, string what, object? actual, object? expected)
	{
		if (!Equals(actual, expected))
		{
			problems.Add($"Slot {p.Slot}: {what} is {actual ?? "null"}, not {expected ?? "null"}");
		}
	}

	private static long Count(string path, string sql) => long.Parse(TestSave.ScalarText(path, sql)!, CultureInfo.InvariantCulture);

	/// <summary>Every row of a table, every column by quote(), sorted.</summary>
	private static string Dump(string path, string table)
	{
		var columns = TestSave.ScalarText(path, $"SELECT group_concat(name, '|') FROM (SELECT name FROM pragma_table_info('{table}') ORDER BY cid)")!.Split('|');
		var row = string.Join(" || '|' || ", columns.Select(c => $"quote(\"{c}\")"));
		return TestSave.ScalarText(path, $"SELECT group_concat(r, char(10)) FROM (SELECT {row} AS r FROM \"{table}\" ORDER BY r)") ?? "";
	}

	/// <summary>Everything under the test's root (saves, snapshots, sidecars), as paths relative to it.</summary>
	private static List<string> Files(TestSave save) =>
		[.. Directory.EnumerateFileSystemEntries(save.RootPath, "*", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(save.RootPath, path))
			.Order(StringComparer.OrdinalIgnoreCase)];
}

/// <summary>The seed-42 league the read-only checks share: created once through CreateLeague, then generated again to compare with.</summary>
public sealed class Seed42League : IAsyncLifetime
{
	internal TestSave Save { get; private set; } = null!;

	internal ISeedData Data { get; private set; } = null!;

	internal OpenSave Created { get; private set; } = null!;

	internal GeneratedLeague Generated { get; private set; } = null!;

	internal List<LeagueCreationStep> Progress { get; } = [];

	internal CapturingLogger<CreateLeague> Log { get; } = new();

	internal long WallMs { get; private set; }

	public async ValueTask InitializeAsync()
	{
		Save = await TestSave.CreateAsync();
		Data = Save.Services.GetRequiredService<ISeedData>();

		var clock = Stopwatch.StartNew();
		var created = await CreateLeagueTests.NewCreateLeague(Save, Data, Data, Log)
			.RunAsync(new NewLeague("Seed 42", 42, 2026), new StepRecorder(Progress), TestContext.Current.CancellationToken);
		WallMs = clock.ElapsedMilliseconds;

		created.IsSuccess.ShouldBeTrue(created.ToString());
		Created = created.Value;
		Generated = new LeagueGenerator(Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System).Generate(42, 2026);
	}

	public ValueTask DisposeAsync() => Save.DisposeAsync();
}

/// <summary>The real data, but with another college list, or a hook on the contract data the writer reads first.</summary>
internal sealed class SeedDataWith(ISeedData data, CollegeList? colleges = null, Action? onContracts = null) : ISeedData
{
	public TeamSet TeamSet => data.TeamSet;

	public CollegeList Colleges => colleges ?? data.Colleges;

	public PositionData Positions => data.Positions;

	public GenerationSettings Generation => data.Generation;

	public JerseyRules JerseyRules => data.JerseyRules;

	public NameList FirstNames => data.FirstNames;

	public NameList LastNames => data.LastNames;

	public HometownList Hometowns => data.Hometowns;

	public IReadOnlyList<string> FamousPeople => data.FamousPeople;

	public ContractData Contracts
	{
		get
		{
			onContracts?.Invoke();
			return data.Contracts;
		}
	}
}

/// <summary>Records each step as it's reported (Progress&lt;T&gt; would post it later).</summary>
internal sealed class StepRecorder(List<LeagueCreationStep> steps) : IProgress<LeagueCreationStep>
{
	public void Report(LeagueCreationStep value) => steps.Add(value);
}

internal sealed record LoggedEvent(int EventID, LogLevel Level, IReadOnlyDictionary<string, object?> Values);

/// <summary>Keeps every event with its template's values.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
	public List<LoggedEvent> Events { get; } = [];

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		var values = state is IReadOnlyList<KeyValuePair<string, object?>> pairs ? pairs.ToDictionary(p => p.Key, p => p.Value) : [];
		Events.Add(new LoggedEvent(eventId.Id, logLevel, values));
	}
}
