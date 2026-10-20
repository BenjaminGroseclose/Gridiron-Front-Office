using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Tests.Saves;

/// <summary>
/// PERSISTENCE.md §2.2, the engine for the M3 Time Machine: close → pre-restore snapshot → replace → reopen, guarded
/// by the Ironman and live-event rules (D-026).
/// </summary>
public sealed class SaveRestoreTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Restoring_brings_back_the_snapshot_and_keeps_the_state_before_as_pre_restore()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync();
        await save.InsertProbeAsync("week 8");
        var week8 = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 8), Ct);
        await save.InsertProbeAsync("week 9");
        var order = new List<string>();
        using var closed = save.Events.Subscribe<SaveClosed>(_ => order.Add("closed"));
        using var opened = save.Events.Subscribe<SaveOpened>(_ => order.Add("opened"));

        var restored = await save.Saves.RestoreAsync(week8, Ct);

        restored.IsSuccess.ShouldBeTrue(restored.ToString());
        restored.Value.ShouldBe(save.Save with { Upgraded = false });
        save.Saves.Current.ShouldBe(restored.Value);
        order.ShouldBe(["closed", "opened"]);
        save.ScalarText("SELECT group_concat(Value) FROM Probe").ShouldBe("week 8");
        save.ScalarText("PRAGMA journal_mode").ShouldBe("wal");   // VACUUM INTO wrote a rollback-journal file
        save.ScalarText("PRAGMA integrity_check").ShouldBe("ok");
        File.Exists(save.Save.FilePath + ".restore-tmp").ShouldBeFalse();

        var preRestore = (await save.Snapshots.ListAsync(save.Save.SaveID, Ct)).Single(s => s.Kind == SnapshotKind.PreRestore);
        TestSave.ScalarText(preRestore.FilePath, "SELECT group_concat(Value) FROM Probe").ShouldBe("week 8,week 9");
        TestSave.ScalarText(week8.FilePath, "SELECT group_concat(Value) FROM Probe").ShouldBe("week 8", "The snapshot itself is never touched.");
    }

    [Fact]
    public async Task Restoring_pre_restore_undoes_the_restore()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync();
        var empty = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        await save.InsertProbeAsync("progress");
        await save.Saves.RestoreAsync(empty, Ct);
        var preRestore = (await save.Snapshots.ListAsync(save.Save.SaveID, Ct)).Single(s => s.Kind == SnapshotKind.PreRestore);

        var undone = await save.Saves.RestoreAsync(preRestore, Ct);

        undone.IsSuccess.ShouldBeTrue(undone.ToString());
        save.ScalarText("SELECT group_concat(Value) FROM Probe").ShouldBe("progress");
    }

    [Fact]
    public async Task Play_time_doesnt_go_back_with_the_league()
    {
        await using var save = await TestSave.CreateAsync();
        var start = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        await save.Writer.ExecuteAsync("Test.PlayALot", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).PlayTimeSeconds = 36_000;
            return Result.Ok();
        }, Ct);

        await save.Saves.RestoreAsync(start, Ct);

        save.Scalar("SELECT PlayTimeSeconds FROM SaveInfo").ShouldBeGreaterThanOrEqualTo(36_000);
    }

    [Fact]
    public async Task An_Ironman_league_cant_be_restored()
    {
        await using var save = await TestSave.CreateAsync();
        var snapshot = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        await save.Writer.ExecuteAsync("Test.MakeIronman", async (uow, ct) =>
        {
            (await uow.Db.SaveInfo.SingleAsync(ct)).IsIronman = true;   // no command can do this; it's set at creation
            return Result.Ok();
        }, Ct);
        var generation = save.Scalar("SELECT Generation FROM SaveInfo");

        var result = await save.Saves.RestoreAsync(snapshot, Ct);

        result.Error!.Code.ShouldBe("IronmanNoRestore");
        save.Saves.Current.ShouldBe(save.Save, "A refused restore leaves the save open.");
        save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(generation, "Nothing was written.");
    }

    [Fact]
    public async Task Restore_waits_until_no_live_event_is_running()
    {
        await using var save = await TestSave.CreateAsync();
        var snapshot = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        save.LiveEvents.IsRunning = true;

        (await save.Saves.RestoreAsync(snapshot, Ct)).Error!.Code.ShouldBe("LiveEventRunning");

        save.LiveEvents.IsRunning = false;
        (await save.Saves.RestoreAsync(snapshot, Ct)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Another_leagues_snapshot_is_refused()
    {
        await using var save = await TestSave.CreateAsync("Mine");
        var other = await save.Saves.CreateAsync("Other", Ct);
        var otherSnapshot = (await save.Snapshots.ListAsync(other.Value.SaveID, Ct)).Single();
        await save.Saves.OpenAsync(save.Save.FilePath, Ct);

        (await save.Saves.RestoreAsync(otherSnapshot, Ct)).Error!.Code.ShouldBe("SnapshotNotForThisSave");

        // The same file presented as this league's snapshot is refused too: its SaveID says otherwise.
        (await save.Saves.RestoreAsync(otherSnapshot with { SaveID = save.Save.SaveID }, Ct)).Error!.Code.ShouldBe("SnapshotNotForThisSave");
        save.Saves.Current!.LeagueName.ShouldBe("Mine");
    }

    [Fact]
    public async Task A_damaged_or_missing_snapshot_is_refused_and_the_save_stays_open()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync();
        await save.InsertProbeAsync("keep me");
        var snapshot = await save.Snapshots.TakeAsync(SnapshotTrigger.Week(2027, 1), Ct);
        await TestSave.ScrambleAsync(snapshot.FilePath);

        (await save.Saves.RestoreAsync(snapshot, Ct)).Error!.Code.ShouldBe("SaveDamaged");

        File.Delete(snapshot.FilePath);
        (await save.Saves.RestoreAsync(snapshot, Ct)).Error!.Code.ShouldBe("SnapshotNotFound");
        save.Saves.Current.ShouldBe(save.Save);
        save.ScalarText("SELECT Value FROM Probe").ShouldBe("keep me");
    }
}
