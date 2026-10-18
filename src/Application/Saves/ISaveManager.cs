using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Results;

namespace GridironFrontOffice.Application.Saves;

/// <summary>
/// Creates, opens, closes, restores and deletes league saves (PERSISTENCE.md §2, §7, §10). One save is open at a time;
/// ISaveWriter and ISaveReader work on it. Expected problems (missing, not a GFO save, damaged, from a newer app,
/// refused by a rule) are results.
/// </summary>
public interface ISaveManager
{
    /// <summary>The open save, or null at the main menu.</summary>
    OpenSave? Current { get; }

    /// <summary>Creates a new save in the saves folder, opens it and takes its <c>created</c> snapshot.</summary>
    Task<Result<OpenSave>> CreateAsync(string leagueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a save: integrity check, then schema check, then (for an older save) a pre-upgrade snapshot and the
    /// migrations. Closes the current save first.
    /// </summary>
    Task<Result<OpenSave>> OpenAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Waits for the running command, checkpoints the WAL and releases the file. Safe to call when nothing is open.</summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts the open save back to one of its snapshots (PERSISTENCE.md §2.2): close → snapshot the current state as
    /// <c>pre-restore</c> → replace the file → reopen. Refused in Ironman leagues and while a live event runs.
    /// </summary>
    Task<Result<OpenSave>> RestoreAsync(SnapshotInfo snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a save to the Recycle Bin with its snapshot folder (PERSISTENCE.md §10). Closes it first if it's open.
    /// Works for unreadable files too. The UI confirms first, naming the league.
    /// </summary>
    Task<Result<Unit>> DeleteAsync(string path, CancellationToken cancellationToken = default);
}

/// <param name="Upgraded">True when opening migrated the file to this app's schema.</param>
public sealed record OpenSave(Guid SaveId, string LeagueName, string FilePath, int SchemaVersion, bool Upgraded);

/// <summary>Raised after a save is opened (or created, or restored). Not part of a command, so it's dispatched by SaveManager.</summary>
public sealed record SaveOpened(OpenSave Save) : IDomainEvent;

/// <summary>Raised after the open save is closed.</summary>
public sealed record SaveClosed(Guid SaveId) : IDomainEvent;

/// <summary>Raised after a save file went to the Recycle Bin. <paramref name="SaveId"/> is null for an unreadable file.</summary>
public sealed record SaveDeleted(string FilePath, Guid? SaveId) : IDomainEvent;

/// <summary>The expected ways save management can fail.</summary>
public static class SaveErrors
{
    public static DomainError NotFound(string fileName) =>
        new("SaveNotFound", $"The save {fileName} doesn't exist.");

    public static DomainError NotAGfoSave(string fileName) =>
        new("NotAGfoSave", $"{fileName} isn't a Gridiron Front Office save.");

    public static DomainError Damaged(string fileName, string detail) =>
        new("SaveDamaged", $"{fileName} is damaged and can't be opened ({detail}).");

    public static DomainError NewerVersion(string fileName, string? appVersion) =>
        new("SaveFromNewerVersion",
            $"{fileName} was saved by a newer version of Gridiron Front Office{(appVersion is { Length: > 0 } ? $" ({appVersion})" : "")}. Update the app to open it.");

    public static DomainError InvalidName(string why) => new("InvalidLeagueName", why);

    public static DomainError IronmanNoRestore { get; } =
        new("IronmanNoRestore", "Ironman leagues can't go back to an earlier point.");

    public static DomainError LiveEventRunning(string action) =>
        new("LiveEventRunning", $"{action} isn't available while a live event is running.");

    public static DomainError SnapshotNotFound(string name) =>
        new("SnapshotNotFound", $"The snapshot {name} doesn't exist anymore.");

    public static DomainError SnapshotNotForThisSave(string name) =>
        new("SnapshotNotForThisSave", $"The snapshot {name} doesn't belong to the open league.");

    public static DomainError DeleteFailed(string fileName, string detail) =>
        new("DeleteFailed", $"{fileName} couldn't be moved to the Recycle Bin ({detail}). Close any program using it and try again.");

    public static DomainError DeleteCancelled(string fileName) =>
        new("DeleteCancelled", $"{fileName} wasn't deleted.");
}
