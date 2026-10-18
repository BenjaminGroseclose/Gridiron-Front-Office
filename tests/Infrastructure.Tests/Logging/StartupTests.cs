using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Tests.Logging;

public sealed partial class StartupTests
{
    [Theory]
    [InlineData("0.0.1+ce3e6f6a9b8c7d6e", "0.0.1", "ce3e6f6")]
    [InlineData("0.3.0", "0.3.0", null)]
    [InlineData("1.0.0-beta.2+abc", "1.0.0-beta.2", "abc")]
    public void The_informational_version_splits_into_version_and_short_commit(string informational, string version, string? commit)
    {
        var identity = AppIdentity.Parse(informational, "Release");

        identity.Version.ShouldBe(version);
        identity.Commit.ShouldBe(commit);
        identity.Display.ShouldBe(commit is null ? version : $"{version}+{commit}");
    }

    [Fact]
    public void The_banner_names_the_build_host_and_settings_without_user_folders()
    {
        var identity = AppIdentity.Parse("0.0.1+ce3e6f6", "Debug");
        var paths = AppPaths.ForCurrentUser();
        var settings = new AppSettings { Appearance = new AppearanceSettings { Density = Density.Compact } };

        var banner = StartupBanner.Build(identity, new HostDescription("Desktop", "155.0.4283.45", "2560x1440 @ 100%, 1 monitor"), paths, settings);

        banner.ShouldStartWith("==== Gridiron Front Office 0.0.1+ce3e6f6 (Debug, Desktop) ====");
        banner.ShouldContain("WebView2 155.0.4283.45");
        banner.ShouldContain("Screen: 2560x1440 @ 100%, 1 monitor");
        banner.ShouldContain(@"Paths: data=%LOCALAPPDATA%\GridironFrontOffice");
        banner.ShouldContain("theme=Dark accent=Team density=Compact logLevel=Information logSql=off simTrace=off");
        banner.ShouldContain($"Session: {identity.SessionID}");
        banner.ShouldNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Case.Insensitive);
    }

    [Fact]
    public void The_logger_writes_the_daily_file_in_the_logs_folder_at_the_configured_level()
    {
        using var temp = new TempFolder();
        var paths = AppPaths.ForRoot(temp.Path).EnsureCreated();
        var identity = AppIdentity.Parse("0.0.1+ce3e6f6", "Debug");

        using (var serilog = GfoLogging.CreateLogger(paths, new AppSettings(), identity))
        {
            var log = new SerilogLoggerFactory(serilog).CreateLogger("GridironFrontOffice.Tests");
            HostLog.Banner(log, "==== banner ====");
            BelowDefaultLevel(log);
            HostLog.Exiting(log, 0, 1.5);
        }

        var file = Path.Combine(paths.Logs, $"gfo-{DateTime.Now:yyyyMMdd}.log");
        File.Exists(file).ShouldBeTrue();
        var text = File.ReadAllText(file);
        text.ShouldContain("[INF] [GridironFrontOffice.Tests] ==== banner ====");
        text.ShouldContain("Exiting (code 0, session 1.5 min)");
        text.ShouldNotContain("dropped");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Below the Information default: dropped")]
    private static partial void BelowDefaultLevel(ILogger logger);
}
