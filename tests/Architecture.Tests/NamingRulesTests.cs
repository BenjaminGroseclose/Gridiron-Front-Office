using System.Text.RegularExpressions;

namespace GridironFrontOffice.Architecture.Tests;

/// <summary>
/// D-069: the abbreviation is written ID (plural IDs) in every name GFO chooses: code, columns, log properties, file
/// names and docs. A lone lowercase id (a local, a route parameter, an HTML attribute) is fine.
/// </summary>
public sealed partial class NamingRulesTests
{
    // Names that belong to someone else, so GFO can't respell them.
    private static readonly HashSet<string> OutsideNames =
    [
        "EventId",       // Microsoft.Extensions.Logging: [LoggerMessage(EventId = …)], the struct, Serilog's property
        "eventId",       // ILogger.Log(…, EventId eventId, …)
        "MigrationId",   // EF Core's __EFMigrationsHistory column
        "SessionId",     // System.Diagnostics.Process.SessionId (the single-instance guard)
    ];

    // Outside members reached through a GFO name: the EventId struct's own property, and Process's own ID property in the
    // single-instance guard.
    private static readonly string[] OutsideMembers = ["eventId.I" + "d", "current.I" + "d", "other.I" + "d"];

    private static readonly string[] TextFiles = [".cs", ".razor", ".js", ".css", ".md", ".json", ".props", ".csproj", ".ps1", ".xaml", ".html", ".txt"];

    private static readonly string[] Folders = ["src", "tests", "tools", "docs", "build", "data"];

    [Fact]
    public void Names_and_docs_write_ID_in_capitals()
    {
        var offending = Files()
            .SelectMany(file => Words().Matches(Readable(file)).Select(m => m.Value)
                .Where(word => MixedCaseToken().IsMatch(word) && !OutsideNames.Contains(word))
                .Select(word => $"{Path.GetRelativePath(RepoLayout.Root.FullName, file)}: {word}"))
            .Distinct()
            .ToList();

        offending.ShouldBeEmpty("Write ID and IDs in capitals (D-069). A name owned by a library goes in OutsideNames.");
    }

    [Theory]
    [InlineData("SaveI" + "d", true)]
    [InlineData("saveI" + "d", true)]
    [InlineData("TypedI" + "ds", true)]
    [InlineData("I" + "dConverter", true)]
    [InlineData("SaveID", false)]
    [InlineData("TypedIDs", false)]
    [InlineData("AppIdentity", false)]
    [InlineData("Idle", false)]
    [InlineData("id", false)]
    public void The_rule_spots_the_mixed_case_spelling_only(string word, bool flagged) =>
        MixedCaseToken().IsMatch(word).ShouldBe(flagged);

    private static IEnumerable<string> Files()
    {
        var root = RepoLayout.Root.FullName;
        var nested = Folders
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories));

        return nested
            .Concat(Directory.EnumerateFiles(root))   // CLAUDE.md, build.ps1 and the props files at the root
            .Where(file => TextFiles.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "artifacts"));
    }

    /// <summary>
    /// The text to check, without the outside members, and (in Markdown) without the banned spelling named as inline
    /// code, which D-069 and CLAUDE.md need to say what's banned.
    /// </summary>
    private static string Readable(string file)
    {
        var text = OutsideMembers.Aggregate(File.ReadAllText(file), (current, member) => current.Replace(member, "", StringComparison.Ordinal));
        return file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? text.Replace("`I" + "d`", "", StringComparison.Ordinal).Replace("`I" + "ds`", "", StringComparison.Ordinal)
            : text;
    }

    [GeneratedRegex("[A-Za-z0-9_]+")]
    private static partial Regex Words();

    // The capital I and lowercase d, followed by anything but a lowercase letter (or by a plural s that ends the token).
    [GeneratedRegex("I" + "d(?=[^a-z]|s(?:[^a-z]|$)|$)")]
    private static partial Regex MixedCaseToken();
}
