namespace CityBuilder.Common;

/// <summary>
/// Deterministic, seedable pseudo-random generator (xorshift64*).
/// <para>
/// The simulation MUST be reproducible: given the same seed and the same command
/// stream, every client must reach an identical state (required for lockstep
/// multiplayer and replay/undo). Never use <see cref="System.Random"/> inside the
/// simulation — it is neither seed-stable across runtimes nor snapshot-friendly.
/// </para>
/// <para>
/// The whole state is a single <see cref="ulong"/>, so it serializes trivially
/// into a save-game or a network snapshot via <see cref="State"/>.
/// </para>
/// </summary>
public sealed class DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(ulong seed)
    {
        // A zero seed would lock xorshift at zero forever; substitute a fixed constant.
        _state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
    }

    /// <summary>Raw generator state. Persist/restore this for deterministic saves and replays.</summary>
    public ulong State
    {
        get => _state;
        set => _state = value == 0UL ? 0x9E3779B97F4A7C15UL : value;
    }

    /// <summary>Next raw 64-bit value.</summary>
    public ulong NextUInt64()
    {
        ulong x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * 0x2545F4914F6CDD1DUL;
    }

    /// <summary>Uniform 32-bit non-negative integer.</summary>
    public uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt64() % range);
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public float NextFloat()
    {
        // Use the high 24 bits for a clean float mantissa.
        return (NextUInt64() >> 40) * (1.0f / 16777216.0f);
    }

    /// <summary>Returns true with the given probability in [0, 1].</summary>
    public bool Chance(float probability) => NextFloat() < probability;
}
