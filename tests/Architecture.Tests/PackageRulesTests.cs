namespace GridironFrontOffice.Architecture.Tests;

/// <summary>Which projects may take which NuGet dependencies (ARCHITECTURE.md §4.1, DATA_ACCESS_GUIDE.md §3.1, LOGGING.md §2).</summary>
public sealed class PackageRulesTests
{
    private static readonly string[] Hosts = ["Desktop", "DevHost", "SimLab"];

    [Theory]
    [InlineData("Core")]
    [InlineData("Simulation")]
    public void Pure_projects_reference_no_packages(string project) =>
        RepoLayout.Product(project).PackageReferences.ShouldBeEmpty($"{project} references nothing but the BCL.");

    [Fact]
    public void Application_references_only_the_EF_Core_core_package_and_abstractions()
    {
        var offending = RepoLayout.Product("Application").PackageReferences
            .Where(p => p != "Microsoft.EntityFrameworkCore" && !IsExtensionsAbstractions(p))
            .ToList();

        offending.ShouldBeEmpty("Application's only data dependency is the EF Core core package, no provider (D-024).");
    }

    [Fact]
    public void Only_Infrastructure_references_the_SQLite_provider()
    {
        var offending = RepoLayout.ProductProjects
            .Where(p => p.ShortName != "Infrastructure"
                        && p.PackageReferences.Any(r => r.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal)
                                                     || r.StartsWith("Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal)
                                                     || r == "Microsoft.EntityFrameworkCore.Relational"))
            .Select(p => p.ShortName)
            .ToList();

        offending.ShouldBeEmpty("SQLite and transaction control stay in Infrastructure (DATA_ACCESS_GUIDE.md §3.1).");
    }

    [Theory]
    [InlineData("UI.Components")]
    [InlineData("UI")]
    public void UI_projects_take_no_data_access_logging_or_host_dependencies(string project)
    {
        var ui = RepoLayout.Product(project);
        string[] forbiddenPrefixes = ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Serilog", "Microsoft.AspNetCore.Components.WebView"];

        ui.PackageReferences
            .Where(p => forbiddenPrefixes.Any(prefix => p.StartsWith(prefix, StringComparison.Ordinal)))
            .ShouldBeEmpty($"{project} talks to Application only. No EF Core, SQLite, Serilog or host packages.");
        ui.UsesWpf.ShouldBeFalse($"{project} must not use WPF (the host stays thin, D-021).");
    }

    [Fact]
    public void Serilog_is_referenced_only_by_Infrastructure_and_the_hosts()
    {
        var offending = RepoLayout.ProductProjects
            .Where(p => p.ShortName != "Infrastructure" && !Hosts.Contains(p.ShortName)
                        && p.PackageReferences.Any(r => r.StartsWith("Serilog", StringComparison.Ordinal)))
            .Select(p => p.ShortName)
            .ToList();

        offending.ShouldBeEmpty("Code logs through ILogger<T>. Serilog is configured only at the composition root (LOGGING.md §2).");
    }

    [Fact]
    public void Only_the_Desktop_host_uses_WPF_and_it_targets_the_Windows_SDK_TFM()
    {
        RepoLayout.ProductProjects.Where(p => p.UsesWpf).Select(p => p.ShortName).ShouldBe(["Desktop"]);

        // On plain net10.0-windows the .NET 10 WPF BlazorWebView crashes at startup (HOST_BENCHMARK.md, gotcha 1).
        RepoLayout.Product("Desktop").TargetFramework.ShouldBe("net10.0-windows10.0.19041.0");
    }

    private static bool IsExtensionsAbstractions(string package) =>
        package.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
        && package.EndsWith(".Abstractions", StringComparison.Ordinal);
}
