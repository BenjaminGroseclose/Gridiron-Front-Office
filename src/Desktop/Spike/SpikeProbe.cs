using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GridironFrontOffice.Desktop.Spike;

/// <summary>
/// Spike S0c: startup and diagnostics probe. When <c>GFO_PROBE</c> names a file, each mark appends
/// "name &lt;tab&gt; milliseconds since process start &lt;tab&gt; detail". Off otherwise.
/// </summary>
internal static class SpikeProbe
{
    private static readonly string? FilePath = Environment.GetEnvironmentVariable("GFO_PROBE");
    private static readonly DateTime ProcessStart = Process.GetCurrentProcess().StartTime;
    private static readonly Lock Gate = new();

    public static bool Enabled => FilePath is not null;

    /// <summary>True when the measurement script wants the app to close itself once the theme is applied.</summary>
    public static bool ExitWhenSettled => Environment.GetEnvironmentVariable("GFO_PROBE_EXIT") == "1";

    public static void Mark(string name, string detail = "")
    {
        if (FilePath is null)
        {
            return;
        }

        var ms = (DateTime.Now - ProcessStart).TotalMilliseconds;
        var line = string.Create(CultureInfo.InvariantCulture, $"{name}\t{ms:F0}\t{detail}{Environment.NewLine}");
        lock (Gate)
        {
            File.AppendAllText(FilePath, line);
        }
    }
}
