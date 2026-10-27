using System.Globalization;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Players;

public sealed class PersonNames(NameList firstNames, NameList lastNames, IReadOnlyList<string> famousPeople)
{
	private readonly WeightedPicker<string> _first = Picker(firstNames);
	private readonly WeightedPicker<string> _last = Picker(lastNames);

	private readonly HashSet<string> _blocked = new(famousPeople, FullNames);

	/// <summary>A first and last name; re-rolls a famous name (F-03 §4.11) or one already in <paramref name="taken"/>.</summary>
	public (string First, string Last) Pick(GfoRandom random, ISet<string> taken)
	{
		while (true)
		{
			var first = _first.Pick(random);
			var last = _last.Pick(random);
			var full = $"{first} {last}";

			if (!_blocked.Contains(full) && !taken.Contains(full))
			{
				return (first, last);
			}
		}
	}

	/// <summary>The comparer for full names: case- and accent-insensitive, so "Tom Brädy" is "Tom Brady".</summary>
	public static StringComparer FullNames { get; } =
		StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

	private static WeightedPicker<string> Picker(NameList names) => new(names.Names.Select(x => (x.Name, (double)x.Weight)));
}
