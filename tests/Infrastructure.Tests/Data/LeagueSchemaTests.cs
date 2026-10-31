using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

/// <summary>
/// M1 WP5 slice 1: the schema a new save gets. The shape is read from a real save's SQL, since the migration is what
/// ships, and the model is checked against the migration (D-090: one InitialCreate, regenerated on every change).
/// </summary>
public sealed class LeagueSchemaTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public void There_is_one_migration_until_1_0()
	{
		using var db = Model();

		db.Database.GetMigrations().ShouldHaveSingleItem().ShouldEndWith("_InitialCreate",
			Case.Sensitive, "D-090: a schema change regenerates InitialCreate instead of adding a migration.");
	}

	[Fact]
	public void The_model_matches_the_migration()
	{
		using var db = Model();

		db.Database.HasPendingModelChanges().ShouldBeFalse("An entity or configuration changed without regenerating InitialCreate (D-090).");
	}

	[Fact]
	public async Task Every_typed_ID_key_is_AUTOINCREMENT_and_no_other_key_is()
	{
		var tables = await TablesAsync("SELECT name, sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'");

		tables.Where(t => t[1]!.Contains("AUTOINCREMENT", StringComparison.Ordinal)).Select(t => t[0]).Order(StringComparer.Ordinal).ToList().ShouldBe(
			["Colleges", "Conferences", "Contracts", "Divisions", "Players", "Teams"],
			customMessage: "Seasons is keyed by the season itself, ContractYears by (ContractID, Season), SaveInfo by a GUID.");
	}

	[Fact]
	public async Task Every_foreign_key_restricts_deletes()
	{
		var keys = await TablesAsync(
			"SELECT m.name, f.\"from\", f.\"table\", f.on_delete FROM sqlite_master m JOIN pragma_foreign_key_list(m.name) f " +
			"WHERE m.type = 'table' ORDER BY m.name, f.\"from\"");

		keys.Select(k => string.Join(' ', k)).ShouldBe(
		[
			"ContractYears ContractID Contracts RESTRICT",
			"Contracts PlayerID Players RESTRICT",
			"Contracts TeamID Teams RESTRICT",
			"Divisions ConferenceID Conferences RESTRICT",
			"Players CollegeID Colleges RESTRICT",
			"Players DraftTeamID Teams RESTRICT",
			"Players TeamID Teams RESTRICT",
			"SaveInfo UserTeamID Teams RESTRICT",
			"Teams DivisionID Divisions RESTRICT",
		], customMessage: "History is kept forever, so a delete never cascades (WP5 §2).");
	}

	[Fact]
	public async Task Players_has_a_REAL_column_named_after_every_measurable_attribute_and_trait()
	{
		var columns = await PlayerColumnsAsync();
		string[] names = [.. Enum.GetNames<Measurable>(), .. Enum.GetNames<PlayerAttribute>(), .. Enum.GetNames<PersonalityTrait>()];

		names.Length.ShouldBe(11 + 42 + 6);
		foreach (var name in names)
		{
			typeof(Player).GetProperty(name).ShouldNotBeNull($"Player.{name}").PropertyType.ShouldBe(typeof(double), $"Player.{name}");
			columns.ShouldContainKey(name);
			columns[name].ShouldBe("REAL", name);
		}
	}

	[Fact]
	public async Task Players_has_the_hidden_ratings_under_their_own_names()
	{
		var columns = await PlayerColumnsAsync();

		foreach (var rating in typeof(HiddenRatings).GetProperties())
		{
			typeof(Player).GetProperty(rating.Name).ShouldNotBeNull($"Player.{rating.Name}").PropertyType.ShouldBe(rating.PropertyType, $"Player.{rating.Name}");
			columns.ShouldContainKey(rating.Name);
		}

		columns["DevelopmentCurve"].ShouldBe("TEXT", "An enum is stored by name.");
	}

	/// <summary>The model alone: listing migrations and comparing the model never opens the connection.</summary>
	private static LeagueDbContext Model() =>
		new(new DbContextOptionsBuilder<LeagueDbContext>().UseSqlite("Data Source=:memory:").Options);

	private static async Task<Dictionary<string, string>> PlayerColumnsAsync() =>
		(await TablesAsync("SELECT name, type FROM pragma_table_info('Players')")).ToDictionary(c => c[0]!, c => c[1]!);

	/// <summary>Every row of a query on a new, closed save, each column as text.</summary>
	private static async Task<List<string?[]>> TablesAsync(string sql)
	{
		await using var save = await TestSave.CreateAsync();
		await save.Saves.CloseAsync(Ct);

		await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = save.Save.FilePath, Pooling = false }.ToString());
		await connection.OpenAsync(Ct);
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		await using var reader = await command.ExecuteReaderAsync(Ct);
		var rows = new List<string?[]>();
		while (await reader.ReadAsync(Ct))
		{
			rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i).ToString())]);
		}

		return rows;
	}
}
