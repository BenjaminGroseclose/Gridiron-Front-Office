namespace GridironFrontOffice.Core.Leagues;

/// <summary>
/// The save's single metadata row (PERSISTENCE.md §5.1). The load screen reads it without opening the league.
/// M0 holds the identity, versions, timestamps and the creation settings that are already decided; the league's
/// position (user team, season, phase, date, seed) arrives with league generation in M1.
/// </summary>
public sealed class SaveInfo
{
    public Guid SaveID { get; set; }

    public string LeagueName { get; set; } = "";

    /// <summary>How many schema migrations the file has (mirrored in <c>PRAGMA user_version</c>).</summary>
    public int SchemaVersion { get; set; }

    public string AppVersionCreated { get; set; } = "";

    public string AppVersionLastOpened { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastPlayedAtUtc { get; set; }

    public long PlayTimeSeconds { get; set; }

    /// <summary>D-026: locked at creation; requires <see cref="ClockMode.Live"/>.</summary>
    public bool IsIronman { get; set; }

    /// <summary>D-022: locked at creation.</summary>
    public ClockMode ClockMode { get; set; } = ClockMode.Live;

    /// <summary>D-044: chosen at creation, changeable in the offseason until the draft starts.</summary>
    public ClockPreset ClockPreset { get; set; } = ClockPreset.Standard;

    /// <summary>D-037: locked at creation.</summary>
    public InjuryFrequency InjuryFrequency { get; set; } = InjuryFrequency.Realistic;

    /// <summary>D-063: locked at creation, off by default.</summary>
    public bool CareerCanEnd { get; set; }

    /// <summary>Set once the league is changed by an editor or import, so the save card can say so.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Incremented by SaveWriter on every commit (D-026 groundwork for rollback detection).</summary>
    public long Generation { get; set; }
}

/// <summary>D-022: how every live event in the save treats time.</summary>
public enum ClockMode
{
    /// <summary>The intended experience: time can be accelerated, never paused.</summary>
    Live,

    /// <summary>A Pause button freezes the whole live event.</summary>
    Pausable,
}

/// <summary>D-044: the draft clock per round (times tentative until Spike S1).</summary>
public enum ClockPreset
{
    Relaxed,
    Standard,
    Pressure,
}

/// <summary>D-037.</summary>
public enum InjuryFrequency
{
    Low,
    Realistic,
    High,
}
