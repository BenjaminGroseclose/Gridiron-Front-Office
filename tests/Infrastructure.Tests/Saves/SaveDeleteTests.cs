using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Infrastructure.Tests.Data;

namespace GridironFrontOffice.Infrastructure.Tests.Saves;

/// <summary>
/// PERSISTENCE.md §10: delete sends the save and its snapshot folder to the Recycle Bin, never deletes permanently.
/// The tests' platform fake stands in for the shell (WindowsPlatformServices was checked by hand, M0_FOUNDATIONS.md WP7).
/// </summary>
public sealed class SaveDeleteTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_deleted_save_goes_to_the_Recycle_Bin_with_its_snapshots()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Saves.CloseAsync(Ct);
        var events = new List<SaveDeleted>();
        using var deleted = save.Events.Subscribe<SaveDeleted>(events.Add);

        var result = await save.Saves.DeleteAsync(save.Save.FilePath, Ct);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        save.Platform.Recycled.ShouldHaveSingleItem().ShouldBe([save.Save.FilePath, save.SnapshotsPath]);
        File.Exists(save.Save.FilePath).ShouldBeFalse();
        Directory.Exists(save.SnapshotsPath).ShouldBeFalse();
        (await save.Catalog.ListAsync(Ct)).ShouldBeEmpty();
        events.ShouldBe([new SaveDeleted(save.Save.FilePath, save.Save.SaveID)]);
    }

    [Fact]
    public async Task Deleting_the_open_save_closes_it_first()
    {
        await using var save = await TestSave.CreateAsync();
        var order = new List<string>();
        using var closed = save.Events.Subscribe<SaveClosed>(_ => order.Add("closed"));
        using var deleted = save.Events.Subscribe<SaveDeleted>(_ => order.Add("deleted"));

        (await save.Saves.DeleteAsync(save.Save.FilePath, Ct)).IsSuccess.ShouldBeTrue();

        save.Saves.Current.ShouldBeNull();
        order.ShouldBe(["closed", "deleted"]);
        save.Platform.Recycled.Single().ShouldNotContain(p => p.EndsWith("-wal", StringComparison.Ordinal), "Closing folded the WAL into the file.");
    }

    [Fact]
    public async Task An_unreadable_file_can_be_deleted_too()
    {
        await using var save = await TestSave.CreateAsync();
        var notes = Path.Combine(save.SavesPath, "Notes.gfo");
        await File.WriteAllTextAsync(notes, "Not a save.", Ct);

        (await save.Saves.DeleteAsync(notes, Ct)).IsSuccess.ShouldBeTrue();

        save.Platform.Recycled.ShouldHaveSingleItem().ShouldBe([notes]);
        save.Saves.Current.ShouldBe(save.Save, "Deleting another file leaves the open save alone.");
    }

    [Fact]
    public async Task A_copy_made_in_Explorer_keeps_the_shared_snapshots()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Saves.CloseAsync(Ct);
        var copy = Path.Combine(save.SavesPath, "Torture League - Copy.gfo");
        File.Copy(save.Save.FilePath, copy);   // same SaveID, so the same snapshot folder

        (await save.Saves.DeleteAsync(save.Save.FilePath, Ct)).IsSuccess.ShouldBeTrue();

        save.Platform.Recycled.ShouldHaveSingleItem().ShouldBe([save.Save.FilePath]);
        Directory.Exists(save.SnapshotsPath).ShouldBeTrue();
    }

    [Fact]
    public async Task When_the_player_says_no_the_save_is_kept()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Saves.CloseAsync(Ct);
        save.Platform.PlayerAgrees = false;   // Windows asked about a permanent delete

        var result = await save.Saves.DeleteAsync(save.Save.FilePath, Ct);

        result.Error!.Code.ShouldBe("DeleteCancelled");
        File.Exists(save.Save.FilePath).ShouldBeTrue();
        (await save.Catalog.ListAsync(Ct)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_missing_file_is_refused()
    {
        await using var save = await TestSave.CreateAsync();

        (await save.Saves.DeleteAsync(Path.Combine(save.SavesPath, "Nope.gfo"), Ct)).Error!.Code.ShouldBe("SaveNotFound");
    }
}
