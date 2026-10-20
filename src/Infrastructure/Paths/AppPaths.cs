using GridironFrontOffice.Application.Platform;

namespace GridironFrontOffice.Infrastructure.Paths;

/// <summary>
/// The file locations from ARCHITECTURE.md §7.3. The real app uses <see cref="ForCurrentUser"/>; the DevHost uses
/// <see cref="ForDevelopment"/> and tests use <see cref="ForRoot"/>, so development never touches real saves.
/// </summary>
public sealed class AppPaths : IAppPaths
{
    public const string ProductFolder = "GridironFrontOffice";
    public const string DevelopmentFolder = "GridironFrontOffice-Dev";

    private AppPaths(string root)
    {
        Root = Path.GetFullPath(root);
        Saves = Path.Combine(Root, "Saves");
        Snapshots = Path.Combine(Root, "Snapshots");
        Logs = Path.Combine(Root, "logs");
        Diagnostics = Path.Combine(Root, "diagnostics");
        WebView2 = Path.Combine(Root, "WebView2");
        SettingsFile = Path.Combine(Root, "settings.json");
    }

    public string Root { get; }

    public string Saves { get; }

    public string Snapshots { get; }

    public string Logs { get; }

    public string Diagnostics { get; }

    public string WebView2 { get; }

    public string SettingsFile { get; }

    /// <summary><c>%LOCALAPPDATA%\GridironFrontOffice</c>: the player's real data.</summary>
    public static AppPaths ForCurrentUser() => ForRoot(Path.Combine(LocalAppData, ProductFolder));

    /// <summary><c>%LOCALAPPDATA%\GridironFrontOffice-Dev</c>: the DevHost's own saves, settings and logs.</summary>
    public static AppPaths ForDevelopment() => ForRoot(Path.Combine(LocalAppData, DevelopmentFolder));

    /// <summary>Any folder, for tests and tools.</summary>
    public static AppPaths ForRoot(string root) => new(root);

    /// <summary>Creates the folders that must exist before anything writes to them.</summary>
    public AppPaths EnsureCreated()
    {
        foreach (var folder in new[] { Root, Saves, Snapshots, Logs, Diagnostics })
        {
            Directory.CreateDirectory(folder);
        }

        return this;
    }

    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
