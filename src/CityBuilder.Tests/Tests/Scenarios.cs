using CityBuilder;
using CityBuilder.Commands.Actions;
using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Tests.Tests;

/// <summary>Shared deterministic scenarios so tests build comparable cities the same way.</summary>
internal static class Scenarios
{
    /// <summary>A residential city seeded with desirability and grown for <paramref name="ticks"/> ticks.</summary>
    public static GameSimulation GrownResidential(ulong seed, int ticks)
    {
        var sim = new GameSimulation(new GameConfig(32, 32, seed));

        HeatMap desirability = sim.HeatMaps.GetOrCreate(HeatMapKind.Desirability);
        for (int y = 8; y <= 24; y++)
        {
            for (int x = 8; x <= 24; x++)
            {
                desirability.AddSource(new GridCoord(x, y), 1.5f);
            }
        }

        sim.Submit(new ZoneAreaCommand(new GridCoord(10, 10), new GridCoord(22, 22), ZoneType.Residential, ZoneDensity.Medium));

        for (int i = 0; i < ticks; i++)
        {
            sim.Step();
        }

        return sim;
    }

    /// <summary>Bootstrap used when reloading a save: definitions/systems only, no world content.</summary>
    public static void Bootstrap(GameSimulation sim)
    {
        // The grown-residential scenario creates no ECS building entities, so the reload needs no
        // definitions; this stays as the seam a real host would fill.
    }

    /// <summary>A unique, freshly-created temp directory for file-touching tests.</summary>
    public static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "polis-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
