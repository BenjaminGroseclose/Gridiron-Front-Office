using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.UI.Diagnostics;

/// <summary>UI diagnostics messages. Event IDs 8000–8999 (LOGGING.md §11).</summary>
internal static partial class UiLog
{
    [LoggerMessage(EventId = 8000, Level = LogLevel.Error, Message = "Page /{Page} failed; showing the recovery panel")]
    public static partial void PageError(ILogger logger, Exception exception, string page);

    [LoggerMessage(EventId = 8001, Level = LogLevel.Error, Message = "JavaScript {Kind}: {ErrorMessage} ({Source}:{Line}:{Column})\n{Stack}")]
    public static partial void ClientError(ILogger logger, string kind, string errorMessage, string source, int line, int column, string stack);

    [LoggerMessage(EventId = 8002, Level = LogLevel.Warning, Message = "More than {Limit} JavaScript errors this session; the rest aren't logged")]
    public static partial void ClientErrorsSuppressed(ILogger logger, int limit);

    [LoggerMessage(EventId = 8003, Level = LogLevel.Warning, Message = "gfo-diagnostics.js isn't loaded by the host page, so JavaScript errors won't be logged")]
    public static partial void DiagnosticsScriptMissing(ILogger logger, Exception exception);
}
