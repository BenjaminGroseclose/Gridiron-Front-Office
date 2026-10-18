using GridironFrontOffice.Application.Saves;
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
        alpha.SaveId.ShouldBe(save.Save.SaveId);
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

    private static string[] Folder(string path) =>
        [.. Directory.GetFiles(path).Select(f => $"{Path.GetFileName(f)} {File.GetLastWriteTimeUtc(f):O} {new FileInfo(f).Length}").Order(StringComparer.Ordinal)];
}
