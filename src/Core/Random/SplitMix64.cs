namespace GridironFrontOffice.Core.Random;

/// <summary>
/// SplitMix64 (public domain): a tiny generator used only to turn one seed into GfoRandom's four state words, plus
/// its finalizer, which Seeds.Derive reuses to scramble a hash (D-012).
/// </summary>
public sealed class SplitMix64
{
    private const ulong Increment = 0x9E3779B97F4A7C15;   // 2^64 divided by the golden ratio
    private ulong _state;

    public SplitMix64(ulong seed)
    {
        _state = seed;
    }

    public ulong Next()
    {
        _state += Increment;
        return Mix(_state);
    }

    /// <summary>Scrambles 64 bits, so inputs that differ by one bit give unrelated outputs.</summary>
    public static ulong Mix(ulong x)
    {
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EB;
        return x ^ (x >> 31);
    }
}
