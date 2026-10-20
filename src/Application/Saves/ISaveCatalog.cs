namespace GridironFrontOffice.Application.Saves;

/// <summary>
/// The load screen's list of saves (PERSISTENCE.md §9). It reads each file without opening the league and never
/// changes a file. An unreadable file gets a card of its own instead of breaking the list.
/// </summary>
public interface ISaveCatalog
{
    /// <summary>Every <c>*.gfo</c> in the saves folder: most recently played first, unreadable files last.</summary>
    Task<IReadOnlyList<SaveCard>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>What a save card can show. Until M1 there's no team, season or record yet.</summary>
public sealed record SaveCard(string FilePath, SaveCardStatus Status)
{
    public string FileName => Path.GetFileName(FilePath);

    public Guid SaveID { get; init; }

    public string LeagueName { get; init; } = "";

    public DateTime CreatedAtUtc { get; init; }

    public DateTime LastPlayedAtUtc { get; init; }

    public long PlayTimeSeconds { get; init; }

    public string AppVersionLastOpened { get; init; } = "";

    public bool IsIronman { get; init; }

    /// <summary>The file's schema version (<c>PRAGMA user_version</c>).</summary>
    public int SchemaVersion { get; init; }

    public long SizeBytes { get; init; }

    /// <summary>Why an <see cref="SaveCardStatus.Unreadable"/> file can't be read, as a sentence for the card.</summary>
    public string? Problem { get; init; }

    /// <summary>Open works (an older save is upgraded on the way).</summary>
    public bool CanOpen => Status is SaveCardStatus.Ready or SaveCardStatus.NeedsUpgrade;
}

public enum SaveCardStatus
{
    /// <summary>Opens as it is.</summary>
    Ready,

    /// <summary>Made by an older app: opening upgrades it, after a <c>pre-upgrade</c> snapshot.</summary>
    NeedsUpgrade,

    /// <summary>Made by a newer app: the card says to update.</summary>
    FromNewerVersion,

    /// <summary>"Can't read this save", with Show in folder.</summary>
    Unreadable,
}

public static class SaveCards
{
    /// <summary>The main menu's Continue: the most recently played save that can be opened, if any.</summary>
    public static SaveCard? ContinueTarget(this IEnumerable<SaveCard> cards) =>
        cards.Where(c => c.CanOpen).MaxBy(c => c.LastPlayedAtUtc);
}
