using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Tests.Leagues;

/// <summary>
/// The leagues the league tests share (M1 WP4 guide §5): seeds 1–20 for season 2026 on the shipped data, generated once
/// for every test class, each by its own generator and with the events it logged.
/// </summary>
internal static class GeneratedLeagues
{
	public const int Season = 2026;

	private static readonly Lazy<IReadOnlyList<LoggedLeague>> TwentyLeagues =
		new(() => [.. Enumerable.Range(1, 20).Select(s => Generate(ShippedSeedData.Instance, (ulong)s))]);

	/// <summary>Seeds 1–20, in seed order.</summary>
	public static IReadOnlyList<LoggedLeague> Twenty => TwentyLeagues.Value;

	public static LoggedLeague Generate(ISeedData data, ulong seed)
	{
		var logger = new ListLogger();
		var league = new LeagueGenerator(data, logger, TimeProvider.System).Generate(seed, Season);
		return new LoggedLeague(seed, league, logger.Events);
	}

	/// <summary>Spearman's rank correlation, ties given their average rank.</summary>
	public static double Spearman(IReadOnlyList<(double X, double Y)> pairs)
	{
		var x = Ranks(pairs.Select(p => p.X).ToList());
		var y = Ranks(pairs.Select(p => p.Y).ToList());
		var (meanX, meanY) = (x.Average(), y.Average());
		var covariance = x.Zip(y).Sum(r => (r.First - meanX) * (r.Second - meanY));
		var spread = Math.Sqrt(x.Sum(r => (r - meanX) * (r - meanX)) * y.Sum(r => (r - meanY) * (r - meanY)));
		return covariance / spread;

		static double[] Ranks(List<double> values)
		{
			var ranks = new double[values.Count];
			var order = Enumerable.Range(0, values.Count).OrderBy(i => values[i]).ToList();

			for (var start = 0; start < order.Count;)
			{
				var end = start;
				while (end + 1 < order.Count && values[order[end + 1]] == values[order[start]])
				{
					end++;
				}

				for (var k = start; k <= end; k++)
				{
					ranks[order[k]] = (start + end) / 2.0;
				}

				start = end + 1;
			}

			return ranks;
		}
	}
}

/// <summary>A generated league and the events its generator logged.</summary>
internal sealed record LoggedLeague(ulong Seed, GeneratedLeague League, IReadOnlyList<LoggedEvent> Events);

internal sealed record LoggedEvent(int EventID, LogLevel Level, IReadOnlyDictionary<string, object?> Values);

/// <summary>Keeps each event's ID, level and named values, so tests read what was logged, not its text.</summary>
internal sealed class ListLogger : ILogger<LeagueGenerator>
{
	public List<LoggedEvent> Events { get; } = [];

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		var values = state is IReadOnlyList<KeyValuePair<string, object?>> pairs ? pairs.ToDictionary(p => p.Key, p => p.Value) : new Dictionary<string, object?>();
		Events.Add(new LoggedEvent(eventId.Id, logLevel, values));
	}
}
