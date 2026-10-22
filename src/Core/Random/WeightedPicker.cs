namespace GridironFrontOffice.Core.Random;

/// <summary>
/// Picks items in proportion to their weights: names by frequency, hometowns by population, archetypes and tiers by
/// share. Build it once per list; each pick is one NextDouble and a binary search over the running totals.
/// </summary>
public sealed class WeightedPicker<T>
{
	private readonly List<(T Item, double Weight)> _items;
	private readonly double _totalWeight;

	public WeightedPicker(IEnumerable<(T Item, double Weight)> items)
	{
		// every weight ≥ 0, at least one > 0, else ArgumentException
		ArgumentNullException.ThrowIfNull(items);

		if (items.Any(i => i.Weight < 0))
		{
			throw new ArgumentException("Weights must be non-negative.", nameof(items));
		}

		if (items.Count(i => i.Weight == 0) > 1)
		{
			throw new ArgumentException("Only allowed one zero weight.", nameof(items));
		}

		_totalWeight = items.Sum(i => i.Weight);
		_items = items.ToList();
	}
	public T Pick(GfoRandom random)
	{
		if (_totalWeight == 0)
		{
			throw new InvalidOperationException("Cannot pick from a list with all zero weights.");
		}

		double randomValue = random.NextDouble() * _totalWeight;
		double runningTotal = 0;

		foreach (var (item, weight) in _items)
		{
			runningTotal += weight;
			if (randomValue < runningTotal)
			{
				return item;
			}
		}

		throw new InvalidOperationException("Failed to pick an item.");
	}
}