using CityBuilder.Common;
using CityBuilder.Grid;
using CityBuilder.Population;

namespace CityBuilder.Zoning.Rules;

/// <summary>
/// The production growth rule: a zoned cell develops when BOTH local conditions (desirability
/// and land value, minus pollution and crime) AND the city-wide RCI demand for its category are
/// favourable, and decays when they turn negative — bounded by the zone's density ceiling.
/// Combining a spatial signal (heat-maps) with a global one (<see cref="DemandModel"/>) is what
/// makes districts fill in where they're both wanted and pleasant. Supersedes the simpler
/// <see cref="ZoneGrowthRule"/>. Deterministic given the shared seeded RNG.
/// </summary>
public sealed class DemandGrowthRule : ICellularAutomatonRule
{
    private readonly DemandModel _demand;

    public DemandGrowthRule(DemandModel demand) => _demand = demand;

    public string Name => "DemandGrowth";

    public ZoneCell Evaluate(
        GridCoord cell,
        in ZoneCell current,
        GridLayer<ZoneCell> source,
        IHeatMapProvider heat,
        DeterministicRandom rng)
    {
        ZoneCell result = current;
        if (!current.IsZoned)
        {
            return result;
        }

        float local = heat.Sample(HeatMapKind.Desirability, cell)
                      + heat.Sample(HeatMapKind.LandValue, cell) * 0.5f
                      - heat.Sample(HeatMapKind.Pollution, cell)
                      - heat.Sample(HeatMapKind.Crime, cell);

        float demand = _demand.DemandFor(current.Type); // [-1,1]

        // Half local desirability + full RCI demand → the cell's growth pressure.
        float pressure = local * 0.5f + demand;
        int ceiling = DensityCeiling(current.Density);

        if (pressure > 0.1f && current.DevelopmentLevel < ceiling)
        {
            float chance = Math.Min(0.9f, 0.1f + pressure * 0.5f);
            if (rng.Chance(chance))
            {
                result.DevelopmentLevel = (byte)Math.Min(ceiling, current.DevelopmentLevel + 1);
            }
        }
        else if (pressure < -0.1f && current.DevelopmentLevel > 0)
        {
            result.DevelopmentLevel = (byte)(current.DevelopmentLevel - 1);
        }

        return result;
    }

    private static int DensityCeiling(ZoneDensity density) => density switch
    {
        ZoneDensity.Low => 64,
        ZoneDensity.Medium => 160,
        ZoneDensity.High => 255,
        _ => 0,
    };
}
