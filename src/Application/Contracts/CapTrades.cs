using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;

namespace GridironFrontOffice.Application.Contracts;

/// <summary>
/// Step 7 (D-081 #1, F-12 §4.8): trades veterans between teams until every team's cap total is inside the band. No
/// contract changes to fit a team; players keep their contracts and their slots. Runs once, at league creation, after
/// step 6 has given every rostered player a contract. Draws nothing.
/// </summary>
public static class CapTrades
{
	/// <summary>
	/// Trades in place and returns how many trades it made. Each is between the two teams furthest apart that can trade,
	/// the highest and the lowest whenever they can: two Veteran contracts at the same position and generation tier, the
	/// pair whose cap hits differ by the amount closest to half the two teams' spread, and never one that leaves either
	/// team with two premium deals at that position. It stops when every team is inside the band, when no two teams have
	/// a pair that would bring one of them nearer it, or after <paramref name="maxTrades"/>.
	/// </summary>
	public static int Balance(IReadOnlyList<List<GeneratedPlayer>> rosters, int season, ContractData contracts, int maxTrades = 200)
	{
		ArgumentNullException.ThrowIfNull(rosters);
		ArgumentNullException.ThrowIfNull(contracts);

		var min = contracts.Utilization.Min * contracts.Cap;
		var max = contracts.Utilization.Max * contracts.Cap;
		var premium = contracts.PremiumShare * contracts.Cap;

		// A player's hit never changes (his contract travels with him), so a team's total moves only by its trades
		var hits = rosters.SelectMany(r => r).ToDictionary(p => p.Slot, p => CapHit(p, season));
		var totals = rosters.Select(r => r.Sum(p => hits[p.Slot])).ToArray();
		var trades = 0;

		while (trades < maxTrades)
		{
			if (totals.Min() >= min && totals.Max() <= max)
			{
				break;
			}

			if (NextTrade(rosters, totals, hits, min, max, premium) is not { } next)
			{
				break;
			}

			var (hi, lo, pair) = next;
			var leaving = rosters[hi][pair.High];
			var arriving = rosters[lo][pair.Low];
			var difference = hits[leaving.Slot] - hits[arriving.Slot];

			// Same position and tier, so each roster keeps its depth order
			rosters[hi][pair.High] = arriving;
			rosters[lo][pair.Low] = leaving;
			totals[hi] -= difference;
			totals[lo] += difference;
			trades++;
		}

		return trades;
	}

	/// <summary>
	/// The two teams furthest apart that have a pair to trade, and the pair (D-087): the highest and the lowest team when
	/// they can trade, as before. Only teams the trade brings nearer the band count, the higher one over it or the lower
	/// one under it, so two teams inside the band never trade. Ties go to the lower team indexes. Null when none can.
	/// </summary>
	private static (int Hi, int Lo, (int High, int Low) Pair)? NextTrade(
		IReadOnlyList<List<GeneratedPlayer>> rosters, long[] totals, Dictionary<int, long> hits, double min, double max, double premium)
	{
		var teams = Enumerable.Range(0, totals.Length).ToList();
		var widestFirst =
			from hi in teams
			from lo in teams
			where totals[hi] > totals[lo] && (totals[hi] > max || totals[lo] < min)
			orderby totals[hi] - totals[lo] descending, hi, lo
			select (hi, lo);

		foreach (var (hi, lo) in widestFirst)
		{
			if (BestPair(rosters[hi], rosters[lo], hits, totals[hi] - totals[lo], premium) is { } pair)
			{
				return (hi, lo, pair);
			}
		}

		return null;
	}

	/// <summary>
	/// The pair to trade, as indexes into the two rosters: the one whose difference in cap hits comes closest to half
	/// the spread, ties to the lower slot on the high team, then on the low team. Null when no pair narrows the spread.
	/// A cap hit over <paramref name="premium"/> is a premium deal.
	/// </summary>
	private static (int High, int Low)? BestPair(List<GeneratedPlayer> high, List<GeneratedPlayer> low, Dictionary<int, long> hits, long spread, double premium)
	{
		(int High, int Low)? best = null;
		var bestKey = (Miss: long.MaxValue, HighSlot: int.MaxValue, LowSlot: int.MaxValue);

		for (var h = 0; h < high.Count; h++)
		{
			for (var l = 0; l < low.Count; l++)
			{
				var (leaving, arriving) = (high[h], low[l]);
				var difference = hits[leaving.Slot] - hits[arriving.Slot];

				// The guide's rule is a positive difference. One of the whole spread or more only swaps which team is
				// high, and the next trade could swap the pair straight back, so it has to be under the spread too.
				if (leaving.Contract?.Type != ContractType.Veteran || arriving.Contract?.Type != ContractType.Veteran
					|| leaving.Position != arriving.Position || leaving.Tier != arriving.Tier
					|| difference <= 0 || difference >= spread)
				{
					continue;
				}

				// D-086: the low team took the biggest deals, so it once ended up paying two $40M quarterbacks
				if (Doubles(low, leaving, arriving) || Doubles(high, arriving, leaving))
				{
					continue;
				}

				// |difference − spread / 2|, doubled to stay in whole dollars
				var key = (Miss: Math.Abs(2 * difference - spread), HighSlot: leaving.Slot, LowSlot: arriving.Slot);

				if (key.CompareTo(bestKey) < 0)
				{
					best = (h, l);
					bestKey = key;
				}
			}
		}

		return best;

		// Whether a team that gives up one player for the other then holds two premium deals at his position
		bool Doubles(List<GeneratedPlayer> team, GeneratedPlayer gaining, GeneratedPlayer losing) =>
			hits[gaining.Slot] > premium
			&& team.Any(p => p.Slot != losing.Slot && p.Position == gaining.Position && hits[p.Slot] > premium);
	}

	/// <summary>This season's cap hit. Step 6 gives every rostered player a contract that covers it.</summary>
	internal static long CapHit(GeneratedPlayer player, int season) =>
		player.Contract?.Seasons.SingleOrDefault(s => s.Season == season)?.CapHit
		?? throw new InvalidOperationException($"Slot {player.Slot} has no contract for {season}: step 6 runs before the cap trades.");
}
