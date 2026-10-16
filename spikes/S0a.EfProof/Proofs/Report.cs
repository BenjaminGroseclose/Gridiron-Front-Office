using System.Globalization;
using System.Text;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Proofs;

/// <summary>Collects pass/fail checks and measurements, echoes them live, and renders a Markdown summary.</summary>
internal sealed class Report
{
    private readonly List<(string Name, bool Passed, string Detail)> _checks = [];
    private readonly List<(string Name, string Value)> _measurements = [];

    public bool AllPassed => _checks.TrueForAll(c => c.Passed);

    public void Check(string name, bool passed, string detail = "")
    {
        _checks.Add((name, passed, detail));
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? " | " + detail : "")}");
    }

    public void Measure(string name, string value)
    {
        _measurements.Add((name, value));
        Console.WriteLine($"TIME  {name}: {value}");
    }

    public string ToMarkdown()
    {
        var md = new StringBuilder();
        md.AppendLine("## Checks").AppendLine().AppendLine("| Result | Check | Detail |").AppendLine("|---|---|---|");
        foreach (var (name, passed, detail) in _checks)
        {
            md.AppendLine(CultureInfo.InvariantCulture, $"| {(passed ? "PASS" : "**FAIL**")} | {name} | {detail} |");
        }

        md.AppendLine().AppendLine("## Measurements").AppendLine().AppendLine("| Measurement | Value |").AppendLine("|---|---|");
        foreach (var (name, value) in _measurements)
        {
            md.AppendLine(CultureInfo.InvariantCulture, $"| {name} | {value} |");
        }

        return md.ToString();
    }
}

internal static class Format
{
    public static string Ms(TimeSpan elapsed) => Ms(elapsed.TotalMilliseconds);

    public static string Ms(double milliseconds) =>
        milliseconds.ToString(milliseconds < 10 ? "N2" : "N0", CultureInfo.InvariantCulture) + " ms";

    public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    public static string Bytes(long bytes) => bytes >= 1024 * 1024
        ? (bytes / 1024d / 1024d).ToString("N1", CultureInfo.InvariantCulture) + " MB"
        : (bytes / 1024d).ToString("N1", CultureInfo.InvariantCulture) + " KB";
}
