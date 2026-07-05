using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Persistence;

/// <summary>
/// Deterministic FNV-1a (64-bit) digest of the simulation's persistent state: tick, RNG,
/// treasury, tax rates, terrain and zoning. Two simulations in the same state produce the same
/// value, so a save/load or a replay is verified by comparing one number. The same digest is
/// the desync detector for lockstep multiplayer: peers exchange checksums every N ticks.
/// </summary>
public static class StateChecksum
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(GameSimulation sim)
    {
        ulong hash = OffsetBasis;

        hash = MixInt64(hash, sim.CurrentTick);
        hash = MixUInt64(hash, sim.Random.State);
        hash = MixInt64(hash, sim.Economy.Balance.Units);

        hash = MixInt32(hash, BitConverter.SingleToInt32Bits(sim.Economy.Taxes.GetRate(ZoneType.Residential)));
        hash = MixInt32(hash, BitConverter.SingleToInt32Bits(sim.Economy.Taxes.GetRate(ZoneType.Commercial)));
        hash = MixInt32(hash, BitConverter.SingleToInt32Bits(sim.Economy.Taxes.GetRate(ZoneType.Industrial)));

        Span<TerrainCell> terrain = sim.Map.Terrain.AsSpan();
        for (int i = 0; i < terrain.Length; i++)
        {
            hash = MixByte(hash, (byte)terrain[i].Elevation);
            hash = MixByte(hash, (byte)(terrain[i].Elevation >> 8));
            hash = MixByte(hash, (byte)terrain[i].Kind);
            hash = MixByte(hash, terrain[i].Flags);
        }

        Span<ZoneCell> zoning = sim.Map.Zoning.AsSpan();
        for (int i = 0; i < zoning.Length; i++)
        {
            hash = MixByte(hash, (byte)zoning[i].Type);
            hash = MixByte(hash, (byte)zoning[i].Density);
            hash = MixByte(hash, zoning[i].DevelopmentLevel);
            hash = MixByte(hash, zoning[i].Occupancy);
        }

        return hash;
    }

    private static ulong MixByte(ulong hash, byte value) => (hash ^ value) * Prime;

    private static ulong MixInt32(ulong hash, int value)
    {
        hash = MixByte(hash, (byte)value);
        hash = MixByte(hash, (byte)(value >> 8));
        hash = MixByte(hash, (byte)(value >> 16));
        hash = MixByte(hash, (byte)(value >> 24));
        return hash;
    }

    private static ulong MixInt64(ulong hash, long value) => MixUInt64(hash, (ulong)value);

    private static ulong MixUInt64(ulong hash, ulong value)
    {
        for (int shift = 0; shift < 64; shift += 8)
        {
            hash = MixByte(hash, (byte)(value >> shift));
        }

        return hash;
    }
}
