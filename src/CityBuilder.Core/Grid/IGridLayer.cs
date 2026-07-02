namespace CityBuilder.Grid;

/// <summary>
/// Non-generic facet of a data layer so heterogeneous layers can be stored in one
/// collection and share bounds/identity metadata.
/// </summary>
public interface IGridLayer
{
    MapLayer Layer { get; }
    int Width { get; }
    int Height { get; }

    /// <summary>True if the coordinate is inside this layer's bounds.</summary>
    bool InBounds(GridCoord coord);

    /// <summary>Reset every cell to the layer's default value.</summary>
    void Clear();
}
