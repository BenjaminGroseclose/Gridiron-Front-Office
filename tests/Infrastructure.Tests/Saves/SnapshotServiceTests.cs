using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Infrastructure.Saves;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Infrastructure.Tests.Saves;

/// <summary>PERSISTENCE.md §2.1: snapshots are consistent VACUUM INTO copies, named by trigger and pruned by the retention rules.</summary>
public sealed class SnapshotServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_save_gets_its_created_snapshot()
    {
        await using var save = await TestSave.CreateAsync();

        var snapshots = await save.Snapshots.ListAsync(save.Save.SaveId, Ct);

        var created = snapshots.ShouldHaveSingleItem();
        (created.Name, created.Kind, created.SaveId).ShouldBe(("created", SnapshotKind.Created, save.Save.SaveId));
        created.FilePath.ShouldBe(Path.Combine(save.SnapshotsPath, "created.gfo"));
        TestSave.ScalarText(created.FilePath, "PRAGMA application_id").ShouldBe(SqliteFile.ApplicationId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        TestSave.ScalarText(created.FilePath, "SELECT LeagueName FROM SaveInfo").ShouldBe(save.Save.LeagueName);
    }

    [Fact]
    public async Task A_snapshot_is_the_state_at_the_moment_it_was_taken()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync();
        await save.InsertProbeAsync("before");

        var snapshot = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 8), Ct);
        await save.InsertProbeAsync("after");

        snapshot.Name.ShouldBe("S2027-W08");
        TestSave.ScalarText(snapshot.FilePath, "PRAGMA integrity_check").ShouldBe("ok");
        TestSave.ScalarText(snapshot.FilePath, "SELECT group_concat(Value) FROM Probe").ShouldBe("before");
        save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(2);
    }

    [Fact]
    public async Task Commands_started_during_a_snapshot_wait_for_it_and_all_commit()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync();

        var snapshot = save.Snapshots.TakeAsync(SnapshotTrigger.TrainingCamp(2027), Ct);
        var writes = Enumerable.Range(0, 20).Select(i => save.InsertProbeAsync($"row {i}")).ToList();
        await Task.WhenAll([snapshot, .. writes]);

        save.Scalar("SELECT COUNT(*) FROM Probe").ShouldBe(20);
        var inSnapshot = int.Parse(TestSave.ScalarText((await snapshot).FilePath, "SELECT COUNT(*) FROM Probe")!, System.Globalization.CultureInfo.InvariantCulture);
        inSnapshot.ShouldBeInRange(0, 20);   // a consistent point between commands, never a half-written one
    }

    [Fact]
    public async Task Each_kind_keeps_only_its_newest_snapshots()
    {
        await using var save = await TestSave.CreateAsync();

        for (var week = 1; week <= 6; week++)
        {
            await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, week), Ct);
        }

        await save.Snapshots.TakeAsync(SnapshotTrigger.PreUpgrade(1), Ct);
        await save.Snapshots.TakeAsync(SnapshotTrigger.PreUpgrade(2), Ct);

        var names = (await save.Snapshots.ListAsync(save.Save.SaveId, Ct)).Select(s => s.Name);
        names.ShouldBe(["pre-upgrade-v2", "S2027-W06", "S2027-W05", "S2027-W04", "S2027-W03", "created"]);   // newest first
    }

    [Fact]
    public async Task Over_budget_phase_snapshots_go_oldest_first_and_the_safety_nets_stay()
    {
        var options = new SnapshotOptions();
        await using var save = await TestSave.CreateAsync(configure: s => s.AddSingleton(options));
        var size = (await save.Snapshots.ListAsync(save.Save.SaveId, Ct)).Single().SizeBytes;
        options.BudgetBytes = size * 5 / 2;   // room for two and a half snapshots

        await save.Snapshots.TakeAsync(SnapshotTrigger.TrainingCamp(2027), Ct);
        await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 2), Ct);

        (await save.Snapshots.ListAsync(save.Save.SaveId, Ct)).Select(s => s.Name).ShouldBe(["S2027-W02", "created"]);

        options.BudgetBytes = 1;   // even a budget nothing fits keeps the safety nets and the snapshot just taken
        await save.Snapshots.TakeAsync(SnapshotTrigger.PreRestore, Ct);
        await save.Snapshots.TakeAsync(SnapshotTrigger.SeasonEnd(2027), Ct);

        (await save.Snapshots.ListAsync(save.Save.SaveId, Ct)).Select(s => s.Name).ShouldBe(["S2027-end", "pre-restore", "created"]);
    }

    [Fact]
    public async Task Files_that_arent_snapshots_are_left_alone()
    {
        await using var save = await TestSave.CreateAsync();
        var notes = Path.Combine(save.SnapshotsPath, "my notes.gfo");
        await File.WriteAllTextAsync(notes, "", Ct);

        await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);

        (await save.Snapshots.ListAsync(save.Save.SaveId, Ct)).Select(s => s.Name).ShouldBe(["S2027-W01", "created"]);
        File.Exists(notes).ShouldBeTrue();
    }

    [Fact]
    public async Task Taking_a_snapshot_needs_an_open_save()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Saves.CloseAsync(Ct);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct));

        error.Message.ShouldContain("No save is open");
    }
}
