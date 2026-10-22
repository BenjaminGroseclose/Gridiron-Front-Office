namespace GridironFrontOffice.Core.Random;

/// <summary>
/// Picks items in proportion to their weights: names by frequency, hometowns by population, archetypes and tiers by
/// share. Build it once per list; each pick is one NextDouble and a binary search over the running totals.
/// </summary>
public sealed class WeightedPicker<T>
{
	public WeightedPicker(IEnumerable<(T Item, double Weight)> items);   // every weight ≥ 0, at least one > 0, else ArgumentException
	public T Pick(GfoRandom random);
}