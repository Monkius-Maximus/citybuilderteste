using CityBuilder.Grid;

namespace CityBuilder.Ecs.Components;

/// <summary>Where an entity sits on the map. Example component — data only, no methods.</summary>
public struct GridPositionComponent : IComponent
{
    public GridCoord Cell;
    public int Elevation;

    public GridPositionComponent(GridCoord cell, int elevation = 0)
    {
        Cell = cell;
        Elevation = elevation;
    }
}
