using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Players;

/// <summary>League generation messages (M1 WP4 guide §4.5). Event IDs 1300–1329; percentages come rounded to one decimal.</summary>
internal static partial class PlayerGenerationLog
{
	/// <summary>The plan's 2 s generation target is read here, not asserted (D-078 #4).</summary>
	[LoggerMessage(EventId = 1300, Level = LogLevel.Information, Message = "League generated for season {Season} from seed {Seed}: {PlayerCount} players in {ElapsedMs} ms")]
	public static partial void LeagueGenerated(ILogger logger, int season, ulong seed, int playerCount, long elapsedMs);

	[LoggerMessage(EventId = 1301, Level = LogLevel.Information, Message = "Cap fitted with {TradeCount} trades: teams at {MinPercent}% to {MaxPercent}% of the cap, mean {MeanPercent}%")]
	public static partial void CapFitted(ILogger logger, int tradeCount, double minPercent, double maxPercent, double meanPercent);

	[LoggerMessage(EventId = 1302, Level = LogLevel.Warning, Message = "{Team} is outside the cap band after the trades: {Percent}% of the cap")]
	public static partial void TeamOutsideCapBand(ILogger logger, string team, double percent);
}
