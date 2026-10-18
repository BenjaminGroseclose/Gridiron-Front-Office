using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Core.Results;

namespace GridironFrontOffice.Application.Saves;

/// <summary>
/// Creates, opens and closes league saves (PERSISTENCE.md §7). One save is open at a time; ISaveWriter and
/// ISaveReader work on it. Expected problems (missing, not a GFO save, damaged, from a newer app) are results.
/// </summary>
public interface ISaveManager
{
    /// <summary>The open save, or null at the main menu.</summary>
    OpenSave? Current { get; }

    /// <summary>Creates a new save in the saves folder and opens it.</summary>
    Task<Result<OpenSave>> CreateAsync(string leagueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a save: integrity check, then schema check, then (for an older save) a pre-upgrade snapshot and the
    /// migrations. Closes the current save first.
    /// </summary>
    Task<Result<OpenSave>> OpenAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Waits for the running command, checkpoints the WAL and releases the file. Safe to call when nothing is open.</summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
}

/// <param name="Upgraded">True when opening migrated the file to this app's schema.</param>
public sealed record OpenSave(Guid SaveId, string LeagueName, string FilePath, int SchemaVersion, bool Upgraded);

/// <summary>Raised after a save is opened (or created). Not part of a command, so it's dispatched by SaveManager.</summary>
public sealed record SaveOpened(OpenSave Save) : IDomainEvent;

/// <summary>Raised after the open save is closed.</summary>
public sealed record SaveClosed(Guid SaveId) : IDomainEvent;

/// <summary>The expected ways opening or creating a save can fail.</summary>
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
}
