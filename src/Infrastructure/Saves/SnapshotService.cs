using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Saves;

/// <summary>The snapshot budget (PERSISTENCE.md §2.1). A setting for it can arrive with the Time Machine (M3).</summary>
internal sealed class SnapshotOptions
{
    public long BudgetBytes { get; set; } = SnapshotPolicy.DefaultBudgetBytes;
}

/// <summary>
/// Takes snapshots with <c>VACUUM INTO</c> into <c>Snapshots\{SaveId}\{name}.gfo</c> and applies the retention rules
/// (<see cref="SnapshotPolicy"/>) after each one. A snapshot's time is its file's write time.
/// </summary>
internal sealed class SnapshotService(
    IAppPaths paths,
    LeagueSessionAccessor sessions,
    SaveWriter writer,
    SnapshotOptions options,
    TimeProvider time,
    ILogger<SnapshotService> log) : ISnapshotService
{
    public async Task<SnapshotInfo> TakeAsync(SnapshotTrigger trigger, CancellationToken cancellationToken = default)
    {
        SnapshotInfo? taken = null;
        await writer.RunExclusiveAsync("taking a snapshot", () => Task.Run(() =>
        {
            var session = sessions.Current;   // throws when no save is open
            taken = Take(session.ConnectionString, SqliteFile.ReadSaveId(session.ConnectionString), trigger);
        }, cancellationToken), cancellationToken).ConfigureAwait(false);

        return taken!;
    }

    public Task<IReadOnlyList<SnapshotInfo>> ListAsync(Guid saveId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SnapshotInfo>>([.. List(saveId).OrderByDescending(s => s.TakenAtUtc)]);

    /// <summary>
    /// Snapshots any database (the open session, or a closed save through <see cref="SqliteFile.ReadOnly"/>), then
    /// prunes. SaveManager calls it directly for <c>pre-upgrade</c> and <c>pre-restore</c>, inside its lifecycle lock.
    /// </summary>
    public SnapshotInfo Take(string sourceConnectionString, Guid saveId, SnapshotTrigger trigger)
    {
        var started = time.GetTimestamp();
        var path = Path.Combine(FolderOf(saveId), trigger.Name + ".gfo");
        SqliteFile.VacuumInto(sourceConnectionString, path);
        var taken = Describe(saveId, path, trigger.Kind);
        var elapsedMs = (long)time.GetElapsedTime(started).TotalMilliseconds;
        DataLog.SnapshotTaken(log, trigger.Name, saveId, elapsedMs, taken.SizeBytes / 1024);

        foreach (var prune in SnapshotPolicy.ToPrune(List(saveId), trigger.Name, options.BudgetBytes))
        {
            File.Delete(prune.FilePath);
            DataLog.SnapshotPruned(log, prune.Name, saveId);
        }

        return taken;
    }

    /// <summary><c>Snapshots\{SaveId}</c>: the folder that goes to the Recycle Bin with its save.</summary>
    public string FolderOf(Guid saveId) => Path.Combine(paths.Snapshots, saveId.ToString());

    private List<SnapshotInfo> List(Guid saveId)
    {
        var folder = FolderOf(saveId);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var snapshots = new List<SnapshotInfo>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.gfo"))
        {
            if (string.Equals(Path.GetExtension(file), ".gfo", StringComparison.OrdinalIgnoreCase)
                && SnapshotTrigger.KindOf(Path.GetFileNameWithoutExtension(file)) is { } kind)
            {
                snapshots.Add(Describe(saveId, file, kind));
            }
        }

        return snapshots;
    }

    private static SnapshotInfo Describe(Guid saveId, string path, SnapshotKind kind)
    {
        var file = new FileInfo(path);
        return new SnapshotInfo(saveId, Path.GetFileNameWithoutExtension(path), kind, path, file.LastWriteTimeUtc, file.Length);
    }
}
