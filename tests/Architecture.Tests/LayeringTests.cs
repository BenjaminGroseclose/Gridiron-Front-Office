using System.Reflection;
using NetArchTest.Rules;

namespace GridironFrontOffice.Architecture.Tests;

/// <summary>
/// Type-level layering rules on the compiled assemblies (NetArchTest). The .csproj rules stop a forbidden reference
/// from being added; these stop code from reaching across layers through a transitive dependency.
/// </summary>
public sealed class LayeringTests
{
    private const string Gfo = "GridironFrontOffice.";

    [Fact]
    public void Core_depends_on_no_other_layer_and_no_framework_beyond_the_BCL() =>
        ShouldNotDependOn("Core",
            Gfo + "Simulation", Gfo + "Application", Gfo + "Infrastructure", Gfo + "UI",
            "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Microsoft.Extensions", "Serilog", "System.Windows");

    [Fact]
    public void Simulation_depends_only_on_Core() =>
        ShouldNotDependOn("Simulation",
            Gfo + "Application", Gfo + "Infrastructure", Gfo + "UI",
            "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Serilog", "System.Windows");

    [Fact]
    public void Application_never_reaches_Infrastructure_UI_SQLite_or_Serilog() =>
        ShouldNotDependOn("Application",
            Gfo + "Infrastructure", Gfo + "UI",
            "Microsoft.Data.Sqlite", "Serilog", "System.Windows");

    [Fact]
    public void UI_Components_stays_football_agnostic() =>
        ShouldNotDependOn("UI.Components",
            Gfo + "Core", Gfo + "Simulation", Gfo + "Application", Gfo + "Infrastructure",
            "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Serilog", "System.Windows");

    [Fact]
    public void UI_never_touches_Infrastructure_EF_Core_SQLite_Serilog_or_WPF() =>
        ShouldNotDependOn("UI",
            Gfo + "Infrastructure",
            "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Serilog", "System.Windows");

    private static void ShouldNotDependOn(string project, params string[] namespaces)
    {
        var result = Types.InAssembly(Assembly.Load(Gfo + project))
            .ShouldNot()
            .HaveDependencyOnAny(namespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"{project} types with forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
