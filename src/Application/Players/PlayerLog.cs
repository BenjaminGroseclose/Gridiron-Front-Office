using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Players;

/// <summary>The players' messages: league generation's 1300–1303 (WP4 §4.5) and the league's player list, 1306 (WP6 §4.4).</summary>
internal static partial class PlayerLog
{
	/// <summary>The plan's 2 s generation target is read here, not asserted (D-078 #4).</summary>
	[LoggerMessage(EventId = 1300, Level = LogLevel.Information, Message = "League generated for season {Season} from seed {Seed}: {PlayerCount} players in {ElapsedMs} ms")]
	public static partial void LeagueGenerated(ILogger logger, int season, ulong seed, int playerCount, long elapsedMs);

	[LoggerMessage(EventId = 1301, Level = LogLevel.Information, Message = "Cap fitted with {TradeCount} trades: teams at {MinPercent}% to {MaxPercent}% of the cap, mean {MeanPercent}%")]
	public static partial void CapFitted(ILogger logger, int tradeCount, double minPercent, double maxPercent, double meanPercent);

	[LoggerMessage(EventId = 1302, Level = LogLevel.Warning, Message = "{Team} is outside the cap band after the trades: {Percent}% of the cap")]
	public static partial void TeamOutsideCapBand(ILogger logger, string team, double percent);

	/// <summary>D-087's fallback, before the trades: the league's whole pay was outside the band, which no trade can fix.</summary>
	[LoggerMessage(EventId = 1303, Level = LogLevel.Information, Message = "League pay scaled by {Factor} to fit the cap band: the teams' mean went from {BeforePercent}% to {AfterPercent}% of the cap")]
	public static partial void LeaguePayScaled(ILogger logger, double factor, double beforePercent, double afterPercent);

	/// <summary>
	/// F-03 §12's 20 ms for perceiving a 2,000-row grid is read here, not asserted (D-078 #4): the time covers the reads and
	/// the perception. The viewer is the user's TeamID, or "none" before the pick.
	/// </summary>
	[LoggerMessage(EventId = 1306, Level = LogLevel.Debug, Message = "Read {Players} players for {Viewer} in {ElapsedMs} ms")]
	public static partial void LeaguePlayersRead(ILogger logger, int players, string viewer, long elapsedMs);
}

