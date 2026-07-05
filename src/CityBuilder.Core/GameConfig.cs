using CityBuilder.Grid;

namespace CityBuilder;

/// <summary>
/// Bootstrap parameters for a new simulation — exactly what the "Found a New City" screen
/// collects (name, map size, seed, terrain). Plain data, no engine dependency. Width/height/
/// seed identify a save; name and terrain preset ride along for display and world generation.
/// </summary>
public readonly struct GameConfig
{
    public readonly int Width;
    public readonly int Height;
    public readonly ulong Seed;
    public readonly double TicksPerSecond;
    public readonly string CityName;
    public readonly TerrainPreset Terrain;

    public GameConfig(
        int width,
        int height,
        ulong seed = 1,
        double ticksPerSecond = 10.0,
        string? cityName = null,
        TerrainPreset terrain = TerrainPreset.VerdantPlains)
    {
        Width = width;
        Height = height;
        Seed = seed;
        TicksPerSecond = ticksPerSecond;
        CityName = string.IsNullOrWhiteSpace(cityName) ? "New City" : cityName!;
        Terrain = terrain;
    }

    public static GameConfig Default => new(64, 64);
}
