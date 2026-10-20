using System.Globalization;
using System.Runtime.InteropServices;
using GridironFrontOffice.Infrastructure.Logging;
using Microsoft.Web.WebView2.Core;

namespace GridironFrontOffice.Desktop.Diagnostics;

/// <summary>The desktop host's line in the startup banner: the WebView2 runtime and the screen (LOGGING.md §7).</summary>
internal static class HostEnvironment
{
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;
    private const int SmCMonitors = 80;

    public static HostDescription Describe() => new("Desktop", WebView2Version(), Screen());

    private static string WebView2Version()
    {
        try
        {
            return CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return "not installed";
        }
    }

    /// <summary>"2560x1440 @ 100%, 3 monitors": the primary screen in physical pixels (the app is per-monitor DPI aware).</summary>
    private static string Screen()
    {
        var scale = GetDpiForSystem() * 100 / 96;
        var monitors = GetSystemMetrics(SmCMonitors);
        return string.Create(CultureInfo.InvariantCulture,
            $"{GetSystemMetrics(SmCxScreen)}x{GetSystemMetrics(SmCyScreen)} @ {scale}%, {monitors} monitor{(monitors == 1 ? "" : "s")}");
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();
}
