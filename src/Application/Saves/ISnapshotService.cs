using System.Globalization;

namespace GridironFrontOffice.Application.Saves;

/// <summary>
/// Snapshots of a save (PERSISTENCE.md §2.1): compact single-file copies made with <c>VACUUM INTO</c>, kept in
/// <c>Snapshots\{SaveID}\{name}.gfo</c>. Restoring one is <see cref="ISaveManager.RestoreAsync"/>.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Snapshots the open save between commands (commands wait for it) and off the caller's thread, then applies the
    /// retention rules. Throws when no save is open.
    /// </summary>
    Task<SnapshotInfo> TakeAsync(SnapshotTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>A save's snapshots, newest first. Files in its folder that aren't snapshots are ignored.</summary>
    Task<IReadOnlyList<SnapshotInfo>> ListAsync(Guid saveID, CancellationToken cancellationToken = default);
}

/// <param name="Name">The file name without <c>.gfo</c>, e.g. <c>S2027-W08</c>.</param>
public sealed record SnapshotInfo(Guid SaveID, string Name, SnapshotKind Kind, string FilePath, DateTime TakenAtUtc, long SizeBytes);

/// <summary>Why a snapshot was taken (PERSISTENCE.md §2.1). M0 takes Created, PreUpgrade and PreRestore; the phase kinds arrive with phases.</summary>
public enum SnapshotKind
{
    Created,
    TrainingCamp,
    Week,
    TradeDeadline,
    Playoffs,
    SeasonEnd,
    PreFreeAgency,
    PreDraft,
    PreUpgrade,
    PreRestore,
}

/// <summary>A snapshot to take: its kind and its file name, built from the season, week or schema version it needs.</summary>
public readonly record struct SnapshotTrigger
{
    private SnapshotTrigger(SnapshotKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    public SnapshotKind Kind { get; }

    public string Name { get; }

    public static SnapshotTrigger Created { get; } = new(SnapshotKind.Created, "created");

    public static SnapshotTrigger PreRestore { get; } = new(SnapshotKind.PreRestore, "pre-restore");

    /// <param name="fromSchema">The schema version the save had before the upgrade.</param>
    public static SnapshotTrigger PreUpgrade(int fromSchema) => new(SnapshotKind.PreUpgrade, Invariant($"pre-upgrade-v{fromSchema}"));

    public static SnapshotTrigger TrainingCamp(int season) => Seasonal(SnapshotKind.TrainingCamp, season, "camp");

    public static SnapshotTrigger Week(int season, int week) => new(SnapshotKind.Week, Invariant($"S{season}-W{week:00}"));

    public static SnapshotTrigger TradeDeadline(int season) => Seasonal(SnapshotKind.TradeDeadline, season, "deadline");

    public static SnapshotTrigger Playoffs(int season) => Seasonal(SnapshotKind.Playoffs, season, "playoffs");

    public static SnapshotTrigger SeasonEnd(int season) => Seasonal(SnapshotKind.SeasonEnd, season, "end");

    public static SnapshotTrigger PreFreeAgency(int season) => Seasonal(SnapshotKind.PreFreeAgency, season, "pre-fa");

    public static SnapshotTrigger PreDraft(int season) => Seasonal(SnapshotKind.PreDraft, season, "pre-draft");

    /// <summary>The kind a snapshot file name belongs to, or null for a file that isn't a snapshot.</summary>
    public static SnapshotKind? KindOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name == Created.Name)
        {
            return SnapshotKind.Created;
        }

        if (name == PreRestore.Name)
        {
            return SnapshotKind.PreRestore;
        }

        if (name.StartsWith("pre-upgrade-v", StringComparison.Ordinal) && IsNumber(name["pre-upgrade-v".Length..]))
        {
            return SnapshotKind.PreUpgrade;
        }

        // S{yyyy}-{suffix}
        if (name.Length < 7 || name[0] != 'S' || name[5] != '-' || !IsNumber(name[1..5]))
        {
            return null;
        }

        var suffix = name[6..];
        return suffix switch
        {
            "camp" => SnapshotKind.TrainingCamp,
            "deadline" => SnapshotKind.TradeDeadline,
            "playoffs" => SnapshotKind.Playoffs,
            "end" => SnapshotKind.SeasonEnd,
            "pre-fa" => SnapshotKind.PreFreeAgency,
            "pre-draft" => SnapshotKind.PreDraft,
            ['W', _, _] when IsNumber(suffix[1..]) => SnapshotKind.Week,
            _ => null,
        };
    }

    private static SnapshotTrigger Seasonal(SnapshotKind kind, int season, string suffix) => new(kind, Invariant($"S{season}-{suffix}"));

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static bool IsNumber(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);
}

/// <summary>The retention rules from PERSISTENCE.md §2.1, as pure functions the snapshot service applies after every snapshot.</summary>
public static class SnapshotPolicy
{
    /// <summary>The default per-league budget: 2 GB.</summary>
    public const long DefaultBudgetBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>How many of each kind are kept (the newest ones).</summary>
    public static int Keep(SnapshotKind kind) => kind switch
    {
        SnapshotKind.Created => 1,
        SnapshotKind.TrainingCamp => 3,     // the last 3 seasons
        SnapshotKind.Week => 4,             // a rolling 4 weeks
        SnapshotKind.TradeDeadline => 2,
        SnapshotKind.Playoffs => 2,
        SnapshotKind.SeasonEnd => 5,        // the last 5 seasons
        SnapshotKind.PreFreeAgency => 2,
        SnapshotKind.PreDraft => 2,
        SnapshotKind.PreUpgrade => 1,       // the newest replaces the older ones
        SnapshotKind.PreRestore => 1,       // undo for the last restore
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Phase snapshots give way when the budget is exceeded. Created, pre-upgrade and pre-restore are safety nets and
    /// never do (each is a single small file).
    /// </summary>
    public static bool GivesWayToBudget(SnapshotKind kind) =>
        kind is not (SnapshotKind.Created or SnapshotKind.PreUpgrade or SnapshotKind.PreRestore);

    /// <summary>
    /// The snapshots to delete after <paramref name="justTaken"/> was taken: beyond each kind's keep count first,
    /// then, while the total is over <paramref name="budgetBytes"/>, phase snapshots oldest first. The snapshot just
    /// taken is never pruned.
    /// </summary>
    public static IReadOnlyList<SnapshotInfo> ToPrune(IReadOnlyList<SnapshotInfo> snapshots, string justTaken, long budgetBytes)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        var prune = new List<SnapshotInfo>();
        foreach (var kind in snapshots.GroupBy(s => s.Kind))
        {
            prune.AddRange(kind
                .OrderByDescending(s => s.Name == justTaken)
                .ThenByDescending(s => s.TakenAtUtc)
                .ThenByDescending(s => s.Name, StringComparer.Ordinal)
                .Skip(Keep(kind.Key)));
        }

        var kept = snapshots.Except(prune).ToList();
        var total = kept.Sum(s => s.SizeBytes);
        var givingWay = kept
            .Where(s => GivesWayToBudget(s.Kind) && s.Name != justTaken)
            .OrderBy(s => s.TakenAtUtc)
            .ThenBy(s => s.Name, StringComparer.Ordinal);
        foreach (var oldest in givingWay)
        {
            if (total <= budgetBytes)
            {
                break;
            }

            prune.Add(oldest);
            total -= oldest.SizeBytes;
        }

        return prune;
    }
}
