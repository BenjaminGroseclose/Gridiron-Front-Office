using System.Buffers.Binary;
using System.Text;

namespace GridironFrontOffice.Core.Random;

/// <summary>
/// Derived seeds (D-012): the league seed fans out into an independent seed for each purpose and each thing it's
/// for, e.g. Derive(seed, SeedPurpose.Generation, season, slot) for one generated player. Changing how one player is
/// drawn can then never shift anyone else's draws. The hash is FNV-1a 64 finished with SplitMix64's Mix: stable
/// across runs and machines, unlike string.GetHashCode or HashCode.Combine, which .NET randomizes per process.
/// </summary>
public static class Seeds
{
	private const ulong FnvOffsetBasis = 0xCBF29CE484222325;
	private const ulong FnvPrime = 0x100000001B3;
	private const byte PartSeparator = 0x1F;   // ASCII "unit separator": marks where the purpose text ends

	/// <summary>A seed for <paramref name="purpose"/> and <paramref name="parts"/>, derived from the league seed.</summary>
	public static ulong Derive(ulong leagueSeed, string purpose, params ReadOnlySpan<long> parts)
	{
		ArgumentException.ThrowIfNullOrEmpty(purpose);

		Span<byte> word = stackalloc byte[sizeof(ulong)];
		var hash = FnvOffsetBasis;

		BinaryPrimitives.WriteUInt64LittleEndian(word, leagueSeed);
		hash = Fnv1a(hash, word);
		hash = Fnv1a(hash, Encoding.UTF8.GetBytes(purpose));

		foreach (var part in parts)
		{
			hash = Fnv1a(hash, PartSeparator);
			BinaryPrimitives.WriteInt64LittleEndian(word, part);
			hash = Fnv1a(hash, word);
		}

		// FNV-1a alone spreads nearby inputs poorly (slot 0 and slot 1 differ in few bits); Mix scrambles them apart.
		return SplitMix64.Mix(hash);
	}

	private static ulong Fnv1a(ulong hash, ReadOnlySpan<byte> bytes)
	{
		foreach (var b in bytes)
		{
			hash = Fnv1a(hash, b);
		}

		return hash;
	}

	private static ulong Fnv1a(ulong hash, byte b) => (hash ^ b) * FnvPrime;
}

/// <summary>
/// The purpose names for Seeds.Derive, in one place: a typo in a purpose string would silently make a different
/// league. Never rename one; that changes every league generated from it.
/// </summary>
public static class SeedPurpose
{
	public const string Generation = "gen";
	public const string Perception = "perception";
	public const string RosterMix = "gen.mix";     // (season, pair)
	public const string TeamStrength = "gen.strength";  // (season)
	public const string Names = "gen.names";       // (season, slot)
	public const string Draft = "gen.draft";       // (season, entry season)
	public const string Jerseys = "gen.jersey";    // (season, team index)
	public const string Contract = "contract";     // (season, slot), F-12 §4.8
}
