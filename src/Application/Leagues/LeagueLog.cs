using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Leagues;

/// <summary>New League's messages (M1 WP5 guide §4.4). Event IDs 1304–1329, after league generation's 1300–1303.</summary>
internal static partial class LeagueLog
{
	/// <summary>
	/// The plan's 5 s New League target is read here, not asserted (D-078 #4): the time covers generation, the write and
	/// the created snapshot. Generation alone is 1300, and the command alone SaveWriter's 2000.
	/// </summary>
	[LoggerMessage(EventId = 1304, Level = LogLevel.Information, Message = "League \"{LeagueName}\" created: {Teams} teams, {Players} players, {Contracts} contracts, seed {Seed}, season {Season}, in {ElapsedMs} ms")]
	public static partial void LeagueCreated(ILogger logger, string leagueName, int teams, int players, int contracts, ulong seed, int season, long elapsedMs);
}
