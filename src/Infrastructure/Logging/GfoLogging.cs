using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// The Serilog configuration both composition roots share (LOGGING.md §2–4). Code everywhere else logs through
/// <c>ILogger&lt;T&gt;</c>; Serilog types stay here and in the hosts (architecture test).
/// </summary>
public static class GfoLogging
{
    public const long FileSizeLimitBytes = 10 * 1024 * 1024;
    public const int RetainedFiles = 14;

    /// <summary>
    /// The main log: <c>logs\gfo-YYYYMMDD.log</c>, daily plus a new file at 10 MB, 14 files kept, written by a
    /// background worker so the UI thread never waits on disk.
    /// </summary>
    public static Logger CreateLogger(IAppPaths paths, AppSettings settings, AppIdentity app, bool debugOutput = false)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(app);

        var minimum = settings.Developer.LogLevel == AppLogLevel.Debug ? LogEventLevel.Debug : LogEventLevel.Information;
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(minimum)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
                settings.Developer.LogSql ? LogEventLevel.Information : LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("AppVersion", app.Display)
            .Enrich.WithProperty("SessionId", app.SessionId)
            .WriteTo.Async(sink => sink.File(
                new GfoLogFormatter(),
                Path.Combine(paths.Logs, "gfo-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: RetainedFiles));

        if (debugOutput)
        {
            configuration.WriteTo.Debug(new GfoLogFormatter());
        }

        return configuration.CreateLogger();
    }
}
