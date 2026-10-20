using GridironFrontOffice.Infrastructure.Paths;

namespace GridironFrontOffice.Infrastructure.Tests.Paths;

public sealed class AppPathsTests
{
    [Fact]
    public void Every_location_sits_under_the_root_with_the_documented_names()
    {
        using var temp = new TempFolder();

        var paths = AppPaths.ForRoot(temp.Path);

        paths.Root.ShouldBe(temp.Path);
        paths.Saves.ShouldBe(Path.Combine(temp.Path, "Saves"));
        paths.Snapshots.ShouldBe(Path.Combine(temp.Path, "Snapshots"));
        paths.Logs.ShouldBe(Path.Combine(temp.Path, "logs"));
        paths.Diagnostics.ShouldBe(Path.Combine(temp.Path, "diagnostics"));
        paths.WebView2.ShouldBe(Path.Combine(temp.Path, "WebView2"));
        paths.SettingsFile.ShouldBe(Path.Combine(temp.Path, "settings.json"));
    }

    [Fact]
    public void EnsureCreated_makes_the_folders_that_get_written_to()
    {
        using var temp = new TempFolder();

        var paths = AppPaths.ForRoot(Path.Combine(temp.Path, "data")).EnsureCreated();

        Directory.Exists(paths.Saves).ShouldBeTrue();
        Directory.Exists(paths.Snapshots).ShouldBeTrue();
        Directory.Exists(paths.Logs).ShouldBeTrue();
        Directory.Exists(paths.Diagnostics).ShouldBeTrue();
    }

    [Fact]
    public void The_development_root_never_points_at_the_players_data()
    {
        var real = AppPaths.ForCurrentUser();
        var development = AppPaths.ForDevelopment();

        real.Root.ShouldEndWith(AppPaths.ProductFolder);
        development.Root.ShouldEndWith(AppPaths.DevelopmentFolder);
        development.Root.ShouldNotBe(real.Root);
        development.Saves.ShouldNotStartWith(real.Root + Path.DirectorySeparatorChar);
    }
}
