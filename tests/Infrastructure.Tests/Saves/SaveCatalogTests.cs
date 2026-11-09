using GridironFrontOffice.Application;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Infrastructure.Tests.Saves;

/// <summary>PERSISTENCE.md §9: the load screen's list. It reads every file without changing any of them.</summary>
public sealed class SaveCatalogTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Saves_are_listed_most_recently_played_first_and_Continue_picks_the_top_one()
    {
        await using var save = await TestSave.CreateAsync("Alpha");
        var bravo = await save.Saves.CreateAsync("Bravo", Ct);
        await save.Saves.CreateAsync("Charlie", Ct);
        await save.Saves.OpenAsync(bravo.Value.FilePath, Ct);   // Bravo is now the most recently played
        await save.Saves.CloseAsync(Ct);

        var cards = await save.Catalog.ListAsync(Ct);

        cards.Select(c => c.LeagueName).ShouldBe(["Bravo", "Charlie", "Alpha"]);
        cards.ContinueTarget()!.LeagueName.ShouldBe("Bravo");
        var alpha = cards.Single(c => c.LeagueName == "Alpha");
        alpha.Status.ShouldBe(SaveCardStatus.Ready);
        alpha.SaveID.ShouldBe(save.Save.SaveID);
        alpha.FilePath.ShouldBe(save.Save.FilePath);
        alpha.AppVersionLastOpened.ShouldBe("0.0.1+test");
        alpha.SchemaVersion.ShouldBe(save.Save.SchemaVersion);
        alpha.IsIronman.ShouldBeFalse();
        alpha.SizeBytes.ShouldBeGreaterThan(0);
        alpha.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        alpha.CreatedAtUtc.ShouldBeLessThanOrEqualTo(alpha.LastPlayedAtUtc);
    }

    [Fact]
    public async Task An_unreadable_file_gets_its_own_card_instead_of_breaking_the_list()
    {
        await using var save = await TestSave.CreateAsync("Good League");
        var damaged = await save.Saves.CreateAsync("Damaged League", Ct);
        await save.Saves.CloseAsync(Ct);
        await TestSave.ScrambleAsync(damaged.Value.FilePath);
        await File.WriteAllTextAsync(Path.Combine(save.SavesPath, "Notes.gfo"), "Not a save, just text.", Ct);
        await File.WriteAllBytesAsync(Path.Combine(save.SavesPath, "Empty.gfo"), [], Ct);
        TestSave.Tamper(Path.Combine(save.SavesPath, "Recipes.gfo"), "CREATE TABLE Recipes (Name TEXT);");   // someone else's SQLite file

        var cards = await save.Catalog.ListAsync(Ct);

        cards[0].LeagueName.ShouldBe("Good League");
        cards.Skip(1).Select(c => (c.FileName, c.Status, c.Problem)).ShouldBe(
        [
            ("Damaged League.gfo", SaveCardStatus.Unreadable, "This save is damaged."),
            ("Empty.gfo", SaveCardStatus.Unreadable, "This file isn't a Gridiron Front Office save."),
            ("Notes.gfo", SaveCardStatus.Unreadable, "This file isn't a Gridiron Front Office save."),
            ("Recipes.gfo", SaveCardStatus.Unreadable, "This file isn't a Gridiron Front Office save."),
        ]);
        cards.ContinueTarget()!.LeagueName.ShouldBe("Good League");
    }

    [Fact]
    public async Task Listing_never_writes_anything_next_to_the_saves()
    {
        await using var save = await TestSave.CreateAsync("First");
        await save.Saves.CreateAsync("Second", Ct);
        await save.Saves.CloseAsync(Ct);
        var before = Folder(save.SavesPath);

        (await save.Catalog.ListAsync(Ct)).Count.ShouldBe(2);

        Folder(save.SavesPath).ShouldBe(before, "A read-only connection to a closed WAL save would leave -wal and -shm files.");
    }

    [Fact]
    public async Task The_open_save_is_listed_with_what_its_WAL_holds()
    {
        await using var save = await TestSave.CreateAsync("Open League");
        await save.Writer.ExecuteAsync("Test.Rename", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).LeagueName = "Renamed League";
            return Result.Ok();
        }, Ct);

        var card = (await save.Catalog.ListAsync(Ct)).Single();

        File.Exists(save.Save.FilePath + "-wal").ShouldBeTrue("The rename is still in the WAL.");
        card.LeagueName.ShouldBe("Renamed League");
        card.Status.ShouldBe(SaveCardStatus.Ready);
    }

    [Fact]
    public async Task Saves_from_older_and_newer_apps_are_flagged()
    {
        await using var save = await TestSave.CreateAsync("Older");
        var newer = await save.Saves.CreateAsync("Newer", Ct);
        await save.Saves.CloseAsync(Ct);
        TestSave.Tamper(save.Save.FilePath, "PRAGMA user_version = 0;");
        TestSave.Tamper(newer.Value.FilePath, $"PRAGMA user_version = {save.Save.SchemaVersion + 1};");

        var cards = await save.Catalog.ListAsync(Ct);

        cards.Single(c => c.LeagueName == "Older").Status.ShouldBe(SaveCardStatus.NeedsUpgrade);
        cards.Single(c => c.LeagueName == "Newer").Status.ShouldBe(SaveCardStatus.FromNewerVersion);
        cards.ContinueTarget()!.LeagueName.ShouldBe("Older", "A newer save can't be opened, so Continue skips it.");
    }

    [Fact]
    public async Task An_unknown_migration_is_from_an_older_build_or_a_newer_app_by_its_timestamp()
    {
        // D-090: InitialCreate is regenerated on every schema change, so the count stays 1 and only the ID tells.
        // An M0 save holds M0's InitialCreate; a later ID than the app's own is a newer app's.
        await using var save = await TestSave.CreateAsync("Stale");
        var future = await save.Saves.CreateAsync("Future", Ct);
        var current = await save.Saves.CreateAsync("Current", Ct);
        await save.Saves.CloseAsync(Ct);
        TestSave.Tamper(save.Save.FilePath, "UPDATE __EFMigrationsHistory SET MigrationId = '20261016174102_InitialCreate';");
        TestSave.Tamper(future.Value.FilePath, "UPDATE __EFMigrationsHistory SET MigrationId = '29991231000000_InitialCreate';");

        var cards = await save.Catalog.ListAsync(Ct);

        var stale = cards.Single(c => c.LeagueName == "Stale");
        stale.Status.ShouldBe(SaveCardStatus.FromOlderBuild, "OpenAsync refuses it, so the list mustn't offer it as ready.");
        stale.SchemaVersion.ShouldBe(current.Value.SchemaVersion);
        stale.CanOpen.ShouldBeFalse();
        cards.Single(c => c.LeagueName == "Future").Status.ShouldBe(SaveCardStatus.FromNewerVersion);
        cards.ContinueTarget()!.LeagueName.ShouldBe("Current");
    }

    [Fact]
    public async Task Only_gfo_files_are_listed_and_odd_file_names_work()
    {
        await using var save = await TestSave.CreateAsync("Team #1 100%");
        await save.Saves.CloseAsync(Ct);
        await File.WriteAllTextAsync(Path.Combine(save.SavesPath, "readme.txt"), "", Ct);
        await File.WriteAllTextAsync(Path.Combine(save.SavesPath, "Other.gfo.restore-tmp"), "", Ct);

        var cards = await save.Catalog.ListAsync(Ct);

        cards.Single().LeagueName.ShouldBe("Team #1 100%");
        cards.Single().Status.ShouldBe(SaveCardStatus.Ready);
    }

    [Fact]
    public async Task With_no_saves_folder_the_list_is_empty()
    {
        using var temp = new TempFolder();
        await using var services = TestSave.BuildServices(temp.Path);

        var cards = await services.GetRequiredService<ISaveCatalog>().ListAsync(Ct);

        cards.ShouldBeEmpty();
        cards.ContinueTarget().ShouldBeNull();
    }

    // M1 WP8 slice 3 (F-01 §4): a Ready save's card shows its league's season, phase and user team
    [Fact]
    public async Task A_leagues_card_has_its_season_and_phase_with_no_team_before_the_pick_and_its_team_after()
    {
        await using var save = await TestSave.CreateAsync("No League", services => services.AddGfoApplication());   // M0's name-only save
        var league = await save.Services.GetRequiredService<CreateLeague>().RunAsync(new NewLeague("Seed 42", 42, 2026), cancellationToken: Ct);
        await save.Saves.CloseAsync(Ct);

        var before = (await save.Catalog.ListAsync(Ct)).Single(c => c.LeagueName == "Seed 42");

        await save.Saves.OpenAsync(league.Value.FilePath, Ct);
        (await save.Services.GetRequiredService<LeagueCommands>().PickUserTeamAsync(new TeamID(5), Ct)).IsSuccess.ShouldBeTrue();
        var team = (await save.Services.GetRequiredService<LeagueQueries>().GetTeamIdentitiesAsync(Ct)).Single(t => t.TeamID == new TeamID(5));
        await save.Saves.CloseAsync(Ct);
        var cards = await save.Catalog.ListAsync(Ct);

        (before.Status, before.Season, before.Phase, before.Team).ShouldBe((SaveCardStatus.Ready, 2026, Phase.TrainingCamp, (TeamIdentityDto?)null));
        var after = cards.Single(c => c.LeagueName == "Seed 42");
        (after.Season, after.Phase, after.Team).ShouldBe((2026, Phase.TrainingCamp, team));
        var empty = cards.Single(c => c.LeagueName == "No League");
        (empty.Status, empty.Season, empty.Phase, empty.Team).ShouldBe((SaveCardStatus.Ready, (int?)null, (Phase?)null, (TeamIdentityDto?)null), "A save with no league in it.");
    }

    [Fact]
    public async Task Only_a_ready_save_shows_its_league_and_a_name_this_build_doesnt_know_shows_none()
    {
        await using var save = await TestSave.CreateAsync("No League", services => services.AddGfoApplication());
        var league = await save.Services.GetRequiredService<CreateLeague>().RunAsync(new NewLeague("Seed 42", 42, 2026), cancellationToken: Ct);
        (await save.Services.GetRequiredService<LeagueCommands>().PickUserTeamAsync(new TeamID(5), Ct)).IsSuccess.ShouldBeTrue();
        await save.Saves.CloseAsync(Ct);
        var tampers = new Dictionary<string, string>
        {
            ["Older.gfo"] = "PRAGMA user_version = 0;",
            ["Newer.gfo"] = "PRAGMA user_version = 99;",
            ["Stale.gfo"] = "UPDATE __EFMigrationsHistory SET MigrationId = '20261016174102_InitialCreate';",
            ["Odd phase.gfo"] = "UPDATE SaveInfo SET CurrentPhase = 'Halftime';",
            ["Numbered phase.gfo"] = "UPDATE SaveInfo SET CurrentPhase = '42';",
            ["Odd crest.gfo"] = "UPDATE Teams SET CrestShape = 'Blob' WHERE TeamID = 5;",
        };
        foreach (var (file, sql) in tampers)
        {
            var copy = Path.Combine(save.SavesPath, file);
            File.Copy(league.Value.FilePath, copy);
            TestSave.Tamper(copy, sql);
        }

        var cards = await save.Catalog.ListAsync(Ct);

        cards.Single(c => c.FilePath == league.Value.FilePath).Team.ShouldNotBeNull("The untouched league shows its team.");
        foreach (var file in tampers.Keys)
        {
            var card = cards.Single(c => c.FileName == file);
            card.Status.ShouldNotBe(SaveCardStatus.Unreadable, $"{file} still gets its card.");
            (card.Season, card.Phase, card.Team).ShouldBe(((int?)null, (Phase?)null, (TeamIdentityDto?)null), file);
        }
    }

    private static string[] Folder(string path) =>
        [.. Directory.GetFiles(path).Select(f => $"{Path.GetFileName(f)} {File.GetLastWriteTimeUtc(f):O} {new FileInfo(f).Length}").Order(StringComparer.Ordinal)];
}
