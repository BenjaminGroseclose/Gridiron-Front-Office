using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// App and host lifecycle messages, shared by both composition roots. Event IDs 1000–1999 (LOGGING.md §11).
/// </summary>
public static partial class HostLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "{Banner}")]
    public static partial void Banner(ILogger logger, string banner);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "{Problem}")]
    public static partial void SettingsUnreadable(ILogger logger, string problem);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Created {SettingsFile} with the defaults")]
    public static partial void SettingsCreated(ILogger logger, string settingsFile);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Main window shown ({Width}x{Height})")]
    public static partial void WindowShown(ILogger logger, double width, double height);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "WebView2 ready in {ElapsedMs} ms (runtime {Version})")]
    public static partial void WebViewReady(ILogger logger, long elapsedMs, string version);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Host page loaded {ElapsedMs} ms after process start")]
    public static partial void PageLoaded(ILogger logger, long elapsedMs);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Exiting (code {ExitCode}, session {Minutes:F1} min)")]
    public static partial void Exiting(ILogger logger, int exitCode, double minutes);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Critical, Message = "Unhandled exception on a background thread (terminating: {IsTerminating})")]
    public static partial void UnhandledException(ILogger logger, Exception exception, bool isTerminating);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Error, Message = "Unobserved task exception")]
    public static partial void UnobservedTaskException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Critical, Message = "Unhandled exception on the UI thread")]
    public static partial void UiThreadException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Crash dialog closed: {Choice}")]
    public static partial void CrashDialogChoice(ILogger logger, string choice);
}
