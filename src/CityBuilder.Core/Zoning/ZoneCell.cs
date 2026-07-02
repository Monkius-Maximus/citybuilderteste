namespace CityBuilder.Zoning;

/// <summary>
/// The zoning state of a single tile (the <see cref="Grid.MapLayer.Zoning"/> plane).
/// A tightly packed 4-byte value type so a whole city's zoning fits in a small,
/// cache-hot array that the cellular-automata pass sweeps every few ticks.
/// </summary>
public struct ZoneCell
{
    public ZoneType Type;
    public ZoneDensity Density;

    /// <summary>Growth progress 0..255. The CA raises/lowers this toward the density ceiling.</summary>
    public byte DevelopmentLevel;

    /// <summary>How occupied the developed structure is (0..255), fed later by the economy.</summary>
    public byte Occupancy;

    public bool IsZoned => Type != ZoneType.None;

    public bool IsDeveloped => DevelopmentLevel > 0;
}
