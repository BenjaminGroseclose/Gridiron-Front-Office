namespace GridironFrontOffice.Application.Settings;

/// <summary>
/// App-wide settings stored in <c>settings.json</c> (ARCHITECTURE.md §7.4). League settings live inside the save.
/// New settings get a default here, so an older file simply picks up the default.
/// </summary>
public sealed record AppSettings
{
    /// <summary>The file format version, bumped only for a breaking change in shape.</summary>
    public int Version { get; init; } = 1;

    public AppearanceSettings Appearance { get; init; } = new();

    public DeveloperSettings Developer { get; init; } = new();
}

/// <summary>Settings → Appearance (VISUAL_DESIGN.md §2, §3.3, §5).</summary>
public sealed record AppearanceSettings
{
    public ThemeChoice Theme { get; init; } = ThemeChoice.Dark;

    public AccentMode Accent { get; init; } = AccentMode.Team;

    public Density Density { get; init; } = Density.Comfortable;
}

/// <summary>Settings → Diagnostics and Developer (LOGGING.md §10).</summary>
public sealed record DeveloperSettings
{
    public AppLogLevel LogLevel { get; init; } = AppLogLevel.Information;

    /// <summary>Log EF Core's SQL text (Information). Off by default: it's noisy.</summary>
    public bool LogSql { get; init; }

    /// <summary>The simulation trace log (LOGGING.md §9). Arrives with the engine in M2.</summary>
    public bool SimTrace { get; init; }
}

/// <summary>Dark is the default (D-031). Match Windows follows the Windows app mode.</summary>
public enum ThemeChoice
{
    Dark,
    Light,
    MatchWindows,
}

/// <summary>Team: the user team's primary color. Neutral: Fluent's brand blue, for loud team colors.</summary>
public enum AccentMode
{
    Team,
    Neutral,
}

public enum Density
{
    Comfortable,
    Compact,
}

/// <summary>The main log's minimum level. Information is the default; Debug adds developer detail.</summary>
public enum AppLogLevel
{
    Information,
    Debug,
}
