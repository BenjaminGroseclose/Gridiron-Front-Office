using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GridironFrontOffice.Infrastructure.Tests.Seed;

/// <summary>
/// Finds the repository (the folder holding GridironFrontOffice.slnx) and reads its test-only blocklists. They hold
/// real names, so they never ship in the app; the tests read them from disk.
/// </summary>
internal static class RepoFiles
{
    // The blocklists use camelCase ("team", "colors"); Web defaults match it.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DirectoryInfo Root { get; } = FindRoot();

    /// <summary>Reads <c>data/blocklists/{fileName}</c>.</summary>
    public static T Blocklist<T>(string fileName) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(Root.FullName, "data", "blocklists", fileName)), JsonOptions)
            ?? throw new InvalidDataException($"{fileName} holds null.");

    /// <summary>A name list from <c>data/blocklists/{fileName}</c>, as <see cref="NameKey"/>s.</summary>
    public static HashSet<string> Names(string fileName) => [.. Blocklist<string[]>(fileName).Select(NameKey)];

    /// <summary>
    /// How the guardrails compare names (F-02 §4.3): trimmed, case-insensitive, accents removed. "Piñon" matches "pinon".
    /// </summary>
    public static string NameKey(string name) =>
        string.Concat(name.Trim().Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToUpperInvariant();

    private static DirectoryInfo FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GridironFrontOffice.slnx")))
            {
                return dir;
            }
        }

        throw new InvalidOperationException($"GridironFrontOffice.slnx not found above {AppContext.BaseDirectory}.");
    }
}

/// <summary>One real NFL team's two colors in <c>metro-nfl-colors.json</c>.</summary>
internal sealed record MetroNflTeam(string Team, IReadOnlyList<string> Colors);
