using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>Aggregate reads over the zoning layer shared by the economy, save metadata and UI.</summary>
public static class ZoningStats
{
    /// <summary>
    /// Population proxy: the sum of residential development levels. The same figure the
    /// economy publishes in <c>BudgetChangedEvent.Population</c> and the Load screen displays.
    /// </summary>
    public static long Population(GridLayer<ZoneCell> zoning)
    {
        long total = 0;
        Span<ZoneCell> cells = zoning.AsSpan();
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].Type == ZoneType.Residential)
            {
                total += cells[i].DevelopmentLevel;
            }
        }

        return total;
    }
}
