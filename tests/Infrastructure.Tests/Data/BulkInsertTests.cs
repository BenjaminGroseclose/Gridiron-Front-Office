using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

/// <summary>
/// D-091: IUnitOfWork.InsertAsync, the bulk insert. A row it writes is the row SaveChanges would write, and it's part
/// of the command's transaction like any other change.
/// </summary>
public sealed class BulkInsertTests : IAsyncLifetime
{
	private TestSave _save = null!;

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public async ValueTask InitializeAsync() => _save = await TestSave.CreateAsync();

	public ValueTask DisposeAsync() => _save.DisposeAsync();

	[Fact]
	public async Task A_row_is_stored_exactly_as_SaveChanges_stores_it()
	{
		// Between them, a player and a contract hold every kind of column: typed IDs (one null), enums, a DateOnly,
		// nullable ints (one null), doubles, longs past int's range and a bool
		var result = await _save.Writer.ExecuteAsync("Test.BulkInsert", async (uow, ct) =>
		{
			await InsertParentsAsync(uow, ct);
			await uow.InsertAsync([NewPlayer(new PlayerID(1))], ct);
			await uow.InsertAsync([NewContract(new ContractID(1), new PlayerID(1))], ct);
			uow.Db.Players.Add(NewPlayer(new PlayerID(2)));
			uow.Db.Contracts.Add(NewContract(new ContractID(2), new PlayerID(2)));
			return Result.Ok();
		}, Ct);

		result.IsSuccess.ShouldBeTrue(result.ToString());
		Row("Players", "PlayerID", 1).ShouldBe(Row("Players", "PlayerID", 2), "The bulk insert's player is EF's.");
		Row("Contracts", "ContractID", 1, "PlayerID").ShouldBe(Row("Contracts", "ContractID", 2, "PlayerID"), "The bulk insert's contract is EF's.");
		_save.ScalarText("SELECT Position || ' ' || Status || ' ' || BirthDate || ' ' || DevelopmentCurve FROM Players WHERE PlayerID = 1")
			.ShouldBe("CB Active 1999-04-12 Late");
		_save.ScalarText("SELECT quote(TeamID) || ' ' || quote(DraftTeamID) FROM Players WHERE PlayerID = 1").ShouldBe("1 NULL");
		_save.ScalarText("SELECT CrestShape || ' ' || StadiumRoof FROM Teams").ShouldBe("Hexagon Retractable");
	}

	[Fact]
	public async Task The_rows_roll_back_with_the_command()
	{
		await Should.ThrowAsync<InvalidOperationException>(() => _save.Writer.ExecuteAsync<Unit>("Test.BulkInsertThrows", async (uow, ct) =>
		{
			await InsertParentsAsync(uow, ct);
			throw new InvalidOperationException("The league didn't fit.");
		}, Ct));

		_save.Scalar("SELECT COUNT(*) FROM Conferences").ShouldBe(0);
		_save.Scalar("SELECT COUNT(*) FROM Teams").ShouldBe(0);
	}

	[Fact]
	public async Task A_row_without_its_generated_key_is_refused()
	{
		var refused = await Should.ThrowAsync<InvalidOperationException>(() => _save.Writer.ExecuteAsync("Test.BulkInsertNoKey", async (uow, ct) =>
		{
			await uow.InsertAsync([new Conference { Key = "EAST", Name = "Eastern Conference" }], ct);
			return Result.Ok();
		}, Ct));

		refused.Message.ShouldContain("ConferenceID");
		_save.Scalar("SELECT COUNT(*) FROM Conferences").ShouldBe(0, "Without the check, the row goes in as ConferenceID 0.");
	}

	[Fact]
	public async Task A_type_outside_the_model_is_refused()
	{
		var refused = await Should.ThrowAsync<InvalidOperationException>(() => _save.Writer.ExecuteAsync("Test.BulkInsertNotAnEntity", async (uow, ct) =>
		{
			await uow.InsertAsync(["not a row"], ct);
			return Result.Ok();
		}, Ct));

		refused.Message.ShouldContain("String isn't an entity");
	}

	/// <summary>A team's chain: conference 1, division 1, team 1, and college 1.</summary>
	private static async Task InsertParentsAsync(IUnitOfWork uow, CancellationToken ct)
	{
		await uow.InsertAsync([new Conference { ConferenceID = new ConferenceID(1), Key = "EAST", Name = "Eastern Conference" }], ct);
		await uow.InsertAsync([new Division { DivisionID = new DivisionID(1), ConferenceID = new ConferenceID(1), Key = "EAST-N", Name = "North" }], ct);
		await uow.InsertAsync(
		[
			new Team
			{
				TeamID = new TeamID(1), DivisionID = new DivisionID(1), Abbreviation = "BAL", City = "Baltimore", Nickname = "Ironclads",
				PrimaryColor = "#1F2A44", SecondaryColor = "#C8A24B", AccentColor = "#FFFFFF",
				CrestShape = CrestShape.Hexagon, CrestMonogram = "B", CrestStripe = CrestStripe.Chevron,
				StadiumName = "Harbor Field", StadiumCapacity = 70_500,
				StadiumRoof = Roof.Retractable, StadiumSurface = Surface.Turf, StadiumClimate = ClimateZone.Temperate,
			},
		], ct);
		await uow.InsertAsync([new College { CollegeID = new CollegeID(1), Name = "Ridgeline State", Region = CollegeRegion.Midwest, Tier = CollegeTier.Power }], ct);
	}

	private static Player NewPlayer(PlayerID id) => new()
	{
		PlayerID = id, TeamID = new TeamID(1), Status = PlayerStatus.Active, Position = Position.CB,
		FirstName = "Avery", LastName = "Stone", BirthDate = new DateOnly(1999, 4, 12), CollegeID = new CollegeID(1),
		HometownName = "Dayton", HometownState = "OH", Experience = 3,
		DraftSeason = 2023, DraftRound = 2, DraftPick = 45, DraftTeamID = null, JerseyNumber = 24,
		Height = 71.5, Forty = 4.41, Speed = 91.25, Press = 77.5,
		Potential = 80.75, DevelopmentCurve = DevelopmentCurve.Late, BustRisk = 0.15, PeakAgeOffset = -0.5, InjuryProneness = 0.3,
		WorkEthic = 0.62, Ego = 0.4,
	};

	private static Contract NewContract(ContractID id, PlayerID player) => new()
	{
		ContractID = id, PlayerID = player, TeamID = new TeamID(1), Type = ContractType.Veteran, SignedSeason = 2025, Years = 4,
		TotalValue = 3_100_000_000, SigningBonus = 2_400_000_000, GuaranteedTotal = 2_900_000_000, IsActive = true,
	};

	/// <summary>Every column of one row, each by SQLite's quote() (which shows the type: 'CB', 1, 4.41, NULL), but the skipped ones.</summary>
	private string Row(string table, string key, int id, params string[] skip)
	{
		var columns = _save.ScalarText($"SELECT group_concat(name, '|') FROM (SELECT name FROM pragma_table_info('{table}') ORDER BY cid)")!
			.Split('|')
			.Where(column => column != key && !skip.Contains(column));

		return _save.ScalarText($"SELECT {string.Join(" || '|' || ", columns.Select(c => $"quote(\"{c}\")"))} FROM \"{table}\" WHERE \"{key}\" = {id}")!;
	}
}
