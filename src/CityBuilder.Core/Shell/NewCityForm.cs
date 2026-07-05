using CityBuilder.Grid;

namespace CityBuilder.Shell;

/// <summary>Map-size cards on the New City screen, exactly as designed.</summary>
public enum MapSizePreset : byte
{
    /// <summary>64 × 64.</summary>
    Hamlet = 0,

    /// <summary>128 × 128 (the default card).</summary>
    Township = 1,

    /// <summary>256 × 256.</summary>
    Metropolis = 2,
}

/// <summary>
/// View-model for "Found a New City". The UI binds its inputs here (city name, size cards,
/// seed field + RANDOMIZE, terrain select); <see cref="CreateConfig"/> turns the form into the
/// <see cref="GameConfig"/> the simulation boots from. Defaults match the approved design
/// (name "Nova Polis", Township, Verdant Plains).
/// </summary>
public sealed class NewCityForm
{
    private readonly Random _uiRandom;

    public NewCityForm(int? randomizerSeed = null)
    {
        // UI-side randomness only (the RANDOMIZE button). The SIMULATION seed is whatever
        // ends up in SeedText — recorded in the config, so runs stay reproducible.
        _uiRandom = randomizerSeed.HasValue ? new Random(randomizerSeed.Value) : new Random();
    }

    public string CityName { get; set; } = "Nova Polis";

    public MapSizePreset MapSize { get; set; } = MapSizePreset.Township;

    /// <summary>Raw seed field text. Digits parse directly; any other text is hashed to a seed.</summary>
    public string SeedText { get; set; } = "1";

    public TerrainPreset Terrain { get; set; } = TerrainPreset.VerdantPlains;

    /// <summary>The RANDOMIZE button: a fresh 6-digit seed, per the design.</summary>
    public void RandomizeSeed() => SeedText = _uiRandom.Next(100_000, 1_000_000).ToString();

    public static int SizeOf(MapSizePreset preset) => preset switch
    {
        MapSizePreset.Hamlet => 64,
        MapSizePreset.Metropolis => 256,
        _ => 128,
    };

    /// <summary>Effective simulation seed derived from the text field.</summary>
    public ulong ResolveSeed()
    {
        if (ulong.TryParse(SeedText, out ulong numeric))
        {
            return numeric == 0 ? 1UL : numeric;
        }

        // Non-numeric input still works: FNV-1a of the text, so "banana" is a valid world.
        ulong hash = 14695981039346656037UL;
        foreach (char c in SeedText)
        {
            hash = (hash ^ c) * 1099511628211UL;
        }

        return hash == 0 ? 1UL : hash;
    }

    /// <summary>FOUND CITY: materialise the form into a bootable config.</summary>
    public GameConfig CreateConfig(double ticksPerSecond = 10.0)
    {
        int size = SizeOf(MapSize);
        return new GameConfig(size, size, ResolveSeed(), ticksPerSecond, CityName, Terrain);
    }
}
