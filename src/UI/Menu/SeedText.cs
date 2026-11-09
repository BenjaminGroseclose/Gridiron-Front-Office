using System.Globalization;

namespace GridironFrontOffice.UI.Menu;

/// <summary>New League's seed field (F-01 §4, D-097): it opens with a random seed, digits are the seed, anything else is refused.</summary>
public static class SeedText
{
	/// <summary>The largest random seed: nine digits, short enough to read out.</summary>
	public const ulong RandomMax = 999_999_999;

	public const string Refused = "A seed is a whole number, up to 18446744073709551615.";

	/// <summary>The seed in the field, or null when it isn't a whole number (an empty field isn't).</summary>
	public static ulong? Read(string? text)
	{
		var trimmed = text?.Trim() ?? "";
		return trimmed.Length > 0 && trimmed.All(char.IsAsciiDigit)
			&& ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var seed) ? seed : null;
	}

	/// <summary>A random seed from 1 to <see cref="RandomMax"/>. The UI's own randomness: generation never sees it.</summary>
	public static ulong Random() => (ulong)System.Random.Shared.NextInt64(1, (long)RandomMax + 1);
}