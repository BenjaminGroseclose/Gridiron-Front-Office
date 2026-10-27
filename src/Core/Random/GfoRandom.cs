using System.Numerics;

namespace GridironFrontOffice.Core.Random;

/// <summary>
/// The one random number generator for Core, Simulation and Application (D-012): xoshiro256** (public domain),
/// seeded through SplitMix64. The same seed gives the same draws on every run. Not thread-safe: use one instance per
/// unit of work, seeded with Seeds.Derive, and never share one between parallel work.
/// </summary>
public sealed class GfoRandom
{
	private ulong _s0;
	private ulong _s1;
	private ulong _s2;
	private ulong _s3;

	/// <summary>Creates a new generator, seeded with the given value.</summary>
	public GfoRandom(ulong seed)
	{
		var seeder = new SplitMix64(seed);
		_s0 = seeder.Next();
		_s1 = seeder.Next();
		_s2 = seeder.Next();
		_s3 = seeder.Next();
	}

	/// <summary>The raw 64 random bits every other draw is built from.</summary>
	public ulong NextUInt64()
	{
		var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;

		var t = _s1 << 17;

		_s2 ^= _s0;
		_s3 ^= _s1;
		_s1 ^= _s2;
		_s0 ^= _s3;

		_s2 ^= t;

		_s3 = BitOperations.RotateLeft(_s3, 45);

		return result;
	}

	/// <summary>A double in [0, 1), using all 53 bits of its mantissa.</summary>
	public double NextDouble()
	{
		return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
	}

	/// <summary>An integer in [minInclusive, maxExclusive), with no modulo bias (Lemire's method).</summary>
	public int Next(int minInclusive, int maxExclusive)
	{
		if (minInclusive >= maxExclusive)
		{
			throw new ArgumentOutOfRangeException(nameof(minInclusive), "minInclusive must be less than maxExclusive");
		}

		// Multiply the 64 random bits by the range: the high half of the 128-bit product is the result.
		var range = (ulong)((long)maxExclusive - minInclusive);
		var high = Math.BigMul(NextUInt64(), range, out var low);
		if (low < range)
		{
			// A few low values would make some results slightly more likely than others: draw again for those.
			var threshold = unchecked(0UL - range) % range;
			while (low < threshold)
			{
				high = Math.BigMul(NextUInt64(), range, out low);
			}
		}

		return (int)(minInclusive + (long)high);
	}

	/// <summary>True with the given probability (0 to 1).</summary>
	public bool Chance(double probability) => NextDouble() < probability;

	/// <summary>
	/// A normally distributed value (Box–Muller). The second value Box–Muller could give is thrown away, so the state
	/// stays only the four words, and the same seed always lines up with the same draws.
	/// </summary>
	public double Normal(double mean = 0, double standardDeviation = 1)
	{
		var u1 = 1.0 - NextDouble();   // (0, 1], so the log below is finite
		var u2 = NextDouble();
		var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
		return mean + (standardDeviation * z);
	}

	/// <summary>
	/// A draw from <paramref name="distribution"/>: always exactly one <see cref="Normal"/> (even when its Sd is 0, so the
	/// data never changes how many draws follow), clamped to its Min–Max when they're given.
	/// </summary>
	public double Draw(Distribution distribution)
	{
		ArgumentNullException.ThrowIfNull(distribution);
		var value = Normal(distribution.Mean, distribution.Sd);
		return Math.Clamp(value, distribution.Min ?? double.NegativeInfinity, distribution.Max ?? double.PositiveInfinity);
	}
}
