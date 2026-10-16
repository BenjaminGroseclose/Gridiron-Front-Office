namespace GridironFrontOffice.Application.Platform;

/// <summary>
/// Where GFO keeps its files (ARCHITECTURE.md §7.3, D-005). Everything goes through this port so the DevHost,
/// tests and SimLab can point it somewhere else and never touch a player's real saves.
/// </summary>
public interface IAppPaths
{
    /// <summary>The data root, normally <c>%LOCALAPPDATA%\GridironFrontOffice</c>.</summary>
    string Root { get; }

    /// <summary>League saves (<c>*.gfo</c>).</summary>
    string Saves { get; }

    /// <summary>Snapshots, one subfolder per save.</summary>
    string Snapshots { get; }

    /// <summary>Rolling log files (<c>gfo-YYYYMMDD.log</c>).</summary>
    string Logs { get; }

    /// <summary>Diagnostics zips and crash material.</summary>
    string Diagnostics { get; }

    /// <summary>The WebView2 profile (user data folder) for the desktop host.</summary>
    string WebView2 { get; }

    /// <summary>The app settings file.</summary>
    string SettingsFile { get; }
}
