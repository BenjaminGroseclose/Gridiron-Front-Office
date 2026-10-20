using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GridironFrontOffice.Infrastructure.Tests.Logging;

/// <summary>
/// settings.json's log level must reach loggers resolved from DI, not just the host's bootstrap logger. .NET's logging
/// layer filters at Information by default, and the DevHost's appsettings.json adds its own levels; AddSerilog's
/// provider-specific rule outranks both, so Serilog alone applies the level from settings.json.
/// </summary>
public sealed partial class LogLevelSettingTests
{
    [Theory]
    [InlineData(false)]   // the desktop host: AddLogging's default Information level
    [InlineData(true)]    // the DevHost: appsettings.json's "Logging:LogLevel:Default": "Information"
    public void Debug_in_settings_reaches_a_DI_logger(bool appsettingsLevels)
    {
        using var temp = new TempFolder();
        var paths = AppPaths.ForRoot(temp.Path).EnsureCreated();
        var settings = new AppSettings { Developer = new DeveloperSettings { LogLevel = AppLogLevel.Debug } };

        using (var serilog = GfoLogging.CreateLogger(paths, settings, AppIdentity.Parse("0.0.1", "Debug")))
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                if (appsettingsLevels)
                {
                    logging.AddFilter(category: null, LogLevel.Information);   // what a "Logging" config section adds
                }

                logging.AddSerilog(serilog);   // as both hosts do
            });
            using var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<LogLevelSettingTests>>();
            DebugDetail(logger);
        }

        File.ReadAllText(Path.Combine(paths.Logs, $"gfo-{DateTime.Now:yyyyMMdd}.log")).ShouldContain("debug detail reached the file");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "debug detail reached the file")]
    private static partial void DebugDetail(Microsoft.Extensions.Logging.ILogger logger);
}
