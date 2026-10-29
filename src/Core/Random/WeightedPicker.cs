namespace GridironFrontOffice.Core.Random;

/// <summary>
/// Picks items in proportion to their weights: names by frequency, hometowns by population, archetypes and tiers by
/// share. Build it once per list; each pick is one NextDouble and a walk along the running totals.
/// </summary>
public sealed class WeightedPicker<T>
{
	private readonly List<(T Item, double Weight)> _items;
	private readonly double _totalWeight;

	public WeightedPicker(IEnumerable<(T Item, double Weight)> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		// Read once: the caller's items may be a Select that would re-run for each check
		_items = items.ToList();

		// every weight ≥ 0, at least one > 0, else ArgumentException
		if (_items.Any(i => i.Weight < 0))
		{
			throw new ArgumentException("Weights must be non-negative.", nameof(items));
		}

		if (!_items.Any(i => i.Weight != 0))
		{
			throw new ArgumentException("Cannot be a list of zeros", nameof(items));
		}

		_totalWeight = _items.Sum(i => i.Weight);
	}

	public T Pick(GfoRandom random)
	{
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