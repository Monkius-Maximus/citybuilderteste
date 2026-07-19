using CityBuilder;
using CityBuilder.Economy;
using CityBuilder.Grid;
using CityBuilder.Population;
using CityBuilder.Tests.Framework;

namespace CityBuilder.Tests.Tests;

public static class EconomyPopulationTests
{
    [TestCase]
    public static void DemandModel_DerivesPopulationFromDevelopment()
    {
        var demand = new DemandModel(DemandSettings.Default);
        demand.Update(1000, 0, 0, averageDesirability: 1.0f, 0.09f, 0.09f, 0.09f);
        Check.Equal(3000L, demand.Population, "population = residentialDev * ResidentsPerDevelopment (3)");
    }

    [TestCase]
    public static void DemandModel_HigherTaxLowersDemand()
    {
        var low = new DemandModel(DemandSettings.Default);
        low.Update(500, 200, 200, 1.0f, 0.05f, 0.05f, 0.05f);

        var high = new DemandModel(DemandSettings.Default);
        high.Update(500, 200, 200, 1.0f, 0.30f, 0.30f, 0.30f);

        Check.True(high.Residential <= low.Residential, "higher tax does not raise residential demand");
        Check.True(high.Commercial <= low.Commercial, "higher tax does not raise commercial demand");
        Check.True(high.Industrial <= low.Industrial, "higher tax does not raise industrial demand");
    }

    [TestCase]
    public static void SectorAccounts_TransfersConserveMoney()
    {
        var ledger = new Ledger();
        var sectors = new SectorAccounts(ledger, DemandSettings.Default);

        long before = sectors.Households.Balance.Units + sectors.Commerce.Balance.Units + sectors.Industry.Balance.Units;
        sectors.Settle(tick: 1, employed: 400, commercialJobs: 200, industrialJobs: 200);
        long after = sectors.Households.Balance.Units + sectors.Commerce.Balance.Units + sectors.Industry.Balance.Units;

        Check.Equal(before, after, "atomic transfers neither create nor destroy money");
    }

    [TestCase]
    public static void Population_GrowsUnderDemand()
    {
        GameSimulation sim = Scenarios.GrownResidential(21, 180);
        Check.True(sim.Demand.Population > 0, "a desirable residential zone gains population");
    }

    [TestCase]
    public static void Terrain_IsDeterministicBySeed()
    {
        var a = new GameSimulation(new GameConfig(48, 48, 777));
        var b = new GameSimulation(new GameConfig(48, 48, 777));
        TerrainGenerator.Generate(a.Map.Terrain, 777, TerrainPreset.VerdantPlains);
        TerrainGenerator.Generate(b.Map.Terrain, 777, TerrainPreset.VerdantPlains);

        Span<TerrainCell> sa = a.Map.Terrain.AsSpan();
        Span<TerrainCell> sb = b.Map.Terrain.AsSpan();
        Check.Equal(sa.Length, sb.Length, "same dimensions");

        bool identical = true;
        for (int i = 0; i < sa.Length; i++)
        {
            if (sa[i].Kind != sb[i].Kind || sa[i].Elevation != sb[i].Elevation)
            {
                identical = false;
                break;
            }
        }

        Check.True(identical, "same seed + preset produce bit-identical terrain");
    }

    [TestCase]
    public static void Terrain_DiffersByPreset()
    {
        var plains = new GameSimulation(new GameConfig(48, 48, 777));
        var coastal = new GameSimulation(new GameConfig(48, 48, 777));
        TerrainGenerator.Generate(plains.Map.Terrain, 777, TerrainPreset.VerdantPlains);
        TerrainGenerator.Generate(coastal.Map.Terrain, 777, TerrainPreset.CoastalReach);

        Check.True(WaterCells(coastal) > WaterCells(plains), "coastal reach has more water than verdant plains");
    }

    private static int WaterCells(GameSimulation sim)
    {
        int water = 0;
        foreach (TerrainCell cell in sim.Map.Terrain.AsSpan())
        {
            if (cell.Kind == TerrainKind.Water)
            {
                water++;
            }
        }

        return water;
    }
}
