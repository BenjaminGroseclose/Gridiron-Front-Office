namespace GridironFrontOffice.Architecture.Tests;

/// <summary>
/// The project dependency graph from ARCHITECTURE.md §4.1, checked against the .csproj files:
/// Desktop/DevHost/SimLab → UI → UI.Components, UI → Application → Simulation → Core, Infrastructure → Application.
/// </summary>
public sealed class ProjectReferenceRulesTests
{
    /// <summary>The projects each product project may reference directly. Anything else is a layering violation.</summary>
    private static readonly Dictionary<string, string[]> MayReference = new(StringComparer.Ordinal)
    {
        ["Core"] = [],
        ["Simulation"] = ["Core"],
        ["Application"] = ["Core", "Simulation"],
        ["Infrastructure"] = ["Core", "Simulation", "Application"],
        ["UI.Components"] = [],
        ["UI"] = ["Core", "Application", "UI.Components"],
        ["Desktop"] = ["Core", "Application", "Infrastructure", "UI.Components", "UI"],
        ["DevHost"] = ["Core", "Application", "Infrastructure", "UI.Components", "UI"],
        ["SimLab"] = ["Core", "Simulation", "Application", "Infrastructure"],
    };

    public static TheoryData<string> ProductProjectNames { get; } = new(RepoLayout.ProductProjects.Select(p => p.ShortName));

    [Fact]
    public void Every_product_project_has_a_dependency_rule_and_every_rule_has_a_project()
    {
        var projects = RepoLayout.ProductProjects.Select(p => p.ShortName).ToList();

        projects.ShouldBe(MayReference.Keys, ignoreOrder: true,
            "A new project under src/ or tools/ needs an entry in MayReference (and in ARCHITECTURE.md §4.1).");
    }

    [Theory]
    [MemberData(nameof(ProductProjectNames))]
    public void Project_references_follow_the_dependency_graph(string project)
    {
        var forbidden = RepoLayout.Product(project).ProjectReferences
            .Except(MayReference[project], StringComparer.Ordinal)
            .ToList();

        forbidden.ShouldBeEmpty($"{project} must not reference {string.Join(", ", forbidden)} (ARCHITECTURE.md §4.1).");
    }
}
