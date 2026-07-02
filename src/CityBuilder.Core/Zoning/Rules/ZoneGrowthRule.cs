using CityBuilder.Common;
using CityBuilder.Grid;

namespace CityBuilder.Zoning.Rules;

/// <summary>
/// Reference growth rule showing how heat-maps drive the automaton. A zoned, buildable
/// cell gains development when net desirability (desirability + land value − pollution −
/// crime) is positive, and decays when it is negative, clamped by the zone's density
/// ceiling. Tuning constants live here so designers can iterate without engine changes.
/// </summary>
public sealed class ZoneGrowthRule : ICellularAutomatonRule
{
    public string Name => "ZoneGrowth";

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

        float desirability = heat.Sample(HeatMapKind.Desirability, cell)
                             + heat.Sample(HeatMapKind.LandValue, cell) * 0.5f;
        float nuisance = heat.Sample(HeatMapKind.Pollution, cell)
                        + heat.Sample(HeatMapKind.Crime, cell);

        float pressure = desirability - nuisance;
        int ceiling = DensityCeiling(current.Density);

        if (pressure > 0.1f && current.DevelopmentLevel < ceiling)
        {
            // Stochastic growth so a district fills in organically rather than in lockstep.
            if (rng.Chance(Math.Min(0.9f, pressure)))
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
