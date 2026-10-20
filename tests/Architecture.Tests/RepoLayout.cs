using System.Xml.Linq;

namespace GridironFrontOffice.Architecture.Tests;

/// <summary>
/// Reads the repository's .csproj files, so reference rules can be checked for every project,
/// including the WPF host that a net10.0 test project can't load.
/// </summary>
internal static class RepoLayout
{
    public static DirectoryInfo Root { get; } = FindRoot();

    /// <summary>Every shipped or tooling project: everything under src/ and tools/.</summary>
    public static IReadOnlyList<ProjectFile> ProductProjects { get; } = LoadProjects("src", "tools");

    public static ProjectFile Product(string shortName) =>
        ProductProjects.Single(p => p.ShortName == shortName);

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

    private static List<ProjectFile> LoadProjects(params string[] folders) =>
        [.. folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(Root.FullName, folder), "*.csproj", SearchOption.AllDirectories))
            .Select(ProjectFile.Load)
            .OrderBy(p => p.ShortName, StringComparer.Ordinal)];
}

/// <summary>The parts of a .csproj the architecture rules care about.</summary>
internal sealed record ProjectFile(
    string ShortName,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences,
    string? TargetFramework,
    bool UsesWpf)
{
    private const string NamePrefix = "GridironFrontOffice.";

    public static ProjectFile Load(string path)
    {
        var elements = XDocument.Load(path).Descendants().ToList();

        List<string> Includes(string itemName) =>
            [.. elements.Where(e => e.Name.LocalName == itemName)
                        .Select(e => (string?)e.Attribute("Include"))
                        .OfType<string>()];

        string? Property(string name) =>
            elements.FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim();

        return new ProjectFile(
            ShortName: ToShortName(path),
            ProjectReferences: [.. Includes("ProjectReference").Select(ToShortName).Order(StringComparer.Ordinal)],
            PackageReferences: [.. Includes("PackageReference").Order(StringComparer.Ordinal)],
            TargetFramework: Property("TargetFramework"),
            UsesWpf: string.Equals(Property("UseWPF"), "true", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>"..\Core\GridironFrontOffice.Core.csproj" → "Core".</summary>
    private static string ToShortName(string projectPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(projectPath.Replace('\\', Path.DirectorySeparatorChar));
        return fileName.StartsWith(NamePrefix, StringComparison.Ordinal) ? fileName[NamePrefix.Length..] : fileName;
    }
}
