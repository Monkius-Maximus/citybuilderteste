using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>
/// Read-only access to the map's scalar fields, handed to cellular-automata rules so
/// they can weigh growth decisions without knowing how the maps are stored or produced.
/// </summary>
public interface IHeatMapProvider
{
    float Sample(HeatMapKind kind, GridCoord cell);

    bool TryGet(HeatMapKind kind, out HeatMap map);
}
