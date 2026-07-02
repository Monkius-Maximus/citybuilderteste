namespace CityBuilder.Grid;

/// <summary>Coarse classification of a terrain tile. Extend as the sim grows.</summary>
public enum TerrainKind : byte
{
    Grass = 0,
    Water = 1,
    Sand = 2,
    Rock = 3,
    Forest = 4,
}

/// <summary>
/// One cell of the <see cref="MapLayer.Terrain"/> plane. A packed value type: eight
/// bytes cover elevation, surface classification and buildability for millions of tiles.
/// </summary>
public struct TerrainCell
{
    public short Elevation;
    public TerrainKind Kind;

    /// <summary>Bit flags reserved for buildable/flooded/protected, etc.</summary>
    public byte Flags;

    public bool IsWater => Kind == TerrainKind.Water;
}
