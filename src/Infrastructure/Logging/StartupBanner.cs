using System.Globalization;
using System.Runtime.InteropServices;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>What the host knows about itself that Infrastructure can't find out (LOGGING.md §7).</summary>
/// <param name="Name">"Desktop" or "DevHost".</param>
/// <param name="WebView2Version">The installed WebView2 runtime, or null when there's no WebView (the DevHost).</param>
/// <param name="Screen">e.g. "2560x1440 @ 100%, 3 monitors".</param>
public sealed record HostDescription(string Name, string? WebView2Version, string Screen);

/// <summary>The block logged once at Information on every launch, so any log can be tied to a build and machine.</summary>
public static class StartupBanner
{
    public static string Build(AppIdentity app, HostDescription host, IAppPaths paths, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);

        var appearance = settings.Appearance;
        var developer = settings.Developer;
        string[] lines =
        [
            $"==== Gridiron Front Office {app.Display} ({app.Configuration}, {host.Name}) ====",
            $"OS: {OperatingSystemName()} {RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()} | {RuntimeInformation.FrameworkDescription} | WebView2 {host.WebView2Version ?? "n/a"}",
            $"Culture: {CultureInfo.CurrentCulture.Name} | Screen: {host.Screen} | Process: {(Environment.Is64BitProcess ? "64" : "32")}-bit",
            $"Paths: data={PathTokens.Tokenize(paths.Root)}",
            Invariant($"Settings: theme={appearance.Theme} accent={appearance.Accent} density={appearance.Density} logLevel={developer.LogLevel} logSql={OnOff(developer.LogSql)} simTrace={OnOff(developer.SimTrace)}"),
            $"Session: {app.SessionId}",
        ];
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>"Windows 11 (10.0.26200)". Windows 11 still reports version 10.0, so the build number tells them apart.</summary>
    private static string OperatingSystemName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return RuntimeInformation.OSDescription;
        }

        var version = Environment.OSVersion.Version;
        var name = version.Major == 10 && version.Build >= 22000 ? "Windows 11" : $"Windows {version.Major}";
        return Invariant($"{name} ({version.Major}.{version.Minor}.{version.Build})");
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
