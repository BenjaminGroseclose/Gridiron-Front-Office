using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>Persistence messages. Event IDs 2000–2999 (LOGGING.md §11).</summary>
internal static partial class DataLog
{
    // Debug, not Information: a live draft runs a command every clock tick, and the log stays under 1 MB/hour.
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "{Operation} committed in {ElapsedMs} ms")]
    public static partial void Committed(ILogger logger, string operation, long elapsedMs);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "{Operation} rejected: {Error}")]
    public static partial void Rejected(ILogger logger, string operation, string error);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "{Operation} rolled back after an exception")]
    public static partial void RolledBack(ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "{Operation} took {ElapsedMs} ms (budget {BudgetMs} ms)")]
    public static partial void Slow(ILogger logger, string operation, long elapsedMs, long budgetMs);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error, Message = "Rolling back {Operation} failed too")]
    public static partial void RollbackFailed(ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Created save \"{LeagueName}\" ({SaveId}) at {Path}")]
    public static partial void SaveCreated(ILogger logger, string leagueName, Guid saveId, string path);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Opened save \"{LeagueName}\" ({SaveId}), schema {SchemaVersion}")]
    public static partial void SaveOpened(ILogger logger, string leagueName, Guid saveId, int schemaVersion);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "Upgraded save {SaveId} from schema {From} to {To} after a snapshot at {Snapshot}")]
    public static partial void SaveUpgraded(ILogger logger, Guid saveId, int from, int to, string snapshot);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Information, Message = "Closed save {SaveId} after {Minutes:F1} min")]
    public static partial void SaveClosed(ILogger logger, Guid saveId, double minutes);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Warning, Message = "Didn't open {File}: {Error}")]
    public static partial void SaveRefused(ILogger logger, string file, string error);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Error, Message = "A {EventType} subscriber failed; the other subscribers still ran")]
    public static partial void EventHandlerFailed(ILogger logger, Exception exception, string eventType);
}
