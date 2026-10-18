using GridironFrontOffice.Application.Saves;

namespace GridironFrontOffice.Application.Tests.Saves;

/// <summary>PERSISTENCE.md §2.1 as code: snapshot names, keep counts and the budget.</summary>
public sealed class SnapshotPolicyTests
{
    private static readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string, SnapshotKind, string> Names => new()
    {
        { "created", SnapshotKind.Created, Name(SnapshotTrigger.Created) },
        { "S2027-camp", SnapshotKind.TrainingCamp, Name(SnapshotTrigger.TrainingCamp(2027)) },
        { "S2027-W08", SnapshotKind.Week, Name(SnapshotTrigger.Week(2027, 8)) },
        { "S2027-W17", SnapshotKind.Week, Name(SnapshotTrigger.Week(2027, 17)) },
        { "S2027-deadline", SnapshotKind.TradeDeadline, Name(SnapshotTrigger.TradeDeadline(2027)) },
        { "S2027-playoffs", SnapshotKind.Playoffs, Name(SnapshotTrigger.Playoffs(2027)) },
        { "S2027-end", SnapshotKind.SeasonEnd, Name(SnapshotTrigger.SeasonEnd(2027)) },
        { "S2028-pre-fa", SnapshotKind.PreFreeAgency, Name(SnapshotTrigger.PreFreeAgency(2028)) },
        { "S2028-pre-draft", SnapshotKind.PreDraft, Name(SnapshotTrigger.PreDraft(2028)) },
        { "pre-upgrade-v3", SnapshotKind.PreUpgrade, Name(SnapshotTrigger.PreUpgrade(3)) },
        { "pre-restore", SnapshotKind.PreRestore, Name(SnapshotTrigger.PreRestore) },
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void Triggers_are_named_per_the_policy_table_and_names_map_back_to_their_kind(string expected, SnapshotKind kind, string actual)
    {
        actual.ShouldBe(expected);
        SnapshotTrigger.KindOf(expected).ShouldBe(kind);
    }

    [Theory]
    [InlineData("my notes")]
    [InlineData("Created")]
    [InlineData("pre-upgrade-v")]
    [InlineData("pre-upgrade-vX")]
    [InlineData("S27-camp")]
    [InlineData("S2027-W8")]
    [InlineData("S2027-lunch")]
    public void Other_names_are_not_snapshots(string name) => SnapshotTrigger.KindOf(name).ShouldBeNull();

    [Fact]
    public void Each_kind_keeps_its_newest_up_to_its_count()
    {
        var weeks = Enumerable.Range(1, 6).Select(w => Snap($"S2027-W{w:00}", SnapshotKind.Week, minutes: w)).ToList();
        var camps = Enumerable.Range(2027, 4).Select(y => Snap($"S{y}-camp", SnapshotKind.TrainingCamp, minutes: y - 2000)).ToList();

        var prune = SnapshotPolicy.ToPrune([.. weeks, .. camps], justTaken: "S2027-W06", long.MaxValue);

        prune.Select(s => s.Name).Order(StringComparer.Ordinal).ShouldBe(["S2027-W01", "S2027-W02", "S2027-camp"]);
    }

    [Fact]
    public void Over_the_budget_phase_snapshots_go_oldest_first_and_never_the_safety_nets()
    {
        List<SnapshotInfo> snapshots =
        [
            Snap("created", SnapshotKind.Created, minutes: 0),
            Snap("pre-upgrade-v1", SnapshotKind.PreUpgrade, minutes: 1),
            Snap("S2027-camp", SnapshotKind.TrainingCamp, minutes: 2),
            Snap("S2027-W01", SnapshotKind.Week, minutes: 3),
            Snap("pre-restore", SnapshotKind.PreRestore, minutes: 4),
            Snap("S2027-W02", SnapshotKind.Week, minutes: 5),
        ];

        SnapshotPolicy.ToPrune(snapshots, "S2027-W02", budgetBytes: 500).Select(s => s.Name).ShouldBe(["S2027-camp"]);
        SnapshotPolicy.ToPrune(snapshots, "S2027-W02", budgetBytes: 0).Select(s => s.Name).ShouldBe(["S2027-camp", "S2027-W01"]);
    }

    [Fact]
    public void The_snapshot_just_taken_is_never_pruned_even_when_its_time_looks_older()
    {
        // The Windows clock was set back: the new W05 carries an earlier time than the four weeks before it.
        var weeks = Enumerable.Range(1, 4).Select(w => Snap($"S2027-W{w:00}", SnapshotKind.Week, minutes: 10 + w)).ToList();
        List<SnapshotInfo> snapshots = [.. weeks, Snap("S2027-W05", SnapshotKind.Week, minutes: 0)];

        SnapshotPolicy.ToPrune(snapshots, "S2027-W05", long.MaxValue).Select(s => s.Name).ShouldBe(["S2027-W01"]);
        SnapshotPolicy.ToPrune(snapshots, "S2027-W05", 0).ShouldNotContain(s => s.Name == "S2027-W05");
    }

    [Fact]
    public void Every_kind_has_a_keep_count_and_the_safety_nets_ignore_the_budget()
    {
        foreach (var kind in Enum.GetValues<SnapshotKind>())
        {
            SnapshotPolicy.Keep(kind).ShouldBeGreaterThan(0);
        }

        Enum.GetValues<SnapshotKind>().Where(k => !SnapshotPolicy.GivesWayToBudget(k))
            .ShouldBe([SnapshotKind.Created, SnapshotKind.PreUpgrade, SnapshotKind.PreRestore]);
    }

    private static string Name(SnapshotTrigger trigger) => trigger.Name;

    private static SnapshotInfo Snap(string name, SnapshotKind kind, int minutes, long size = 100) =>
        new(Guid.Empty, name, kind, $@"C:\Snapshots\{name}.gfo", Start.AddMinutes(minutes), size);
}
