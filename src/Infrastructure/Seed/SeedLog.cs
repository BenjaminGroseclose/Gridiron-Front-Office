using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Infrastructure.Seed;

/// <summary>Built-in league data messages. Event IDs 1200–1219 (F-02 §11).</summary>
internal static partial class SeedLog
{
    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "Team set {Name} loaded: {TeamCount} teams in {ElapsedMs} ms")]
    public static partial void TeamSetLoaded(ILogger logger, string name, int teamCount, long elapsedMs);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "College list {Name} loaded: {CollegeCount} colleges in {ElapsedMs} ms")]
    public static partial void CollegeListLoaded(ILogger logger, string name, int collegeCount, long elapsedMs);
}
