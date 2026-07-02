using CityBuilder.Zoning;

namespace CityBuilder.Grid;

/// <summary>
/// The spatial backbone of the simulation: one shared W×H footprint with several
/// typed data layers stacked on it (terrain, zoning, heat-maps, ...). Systems fetch
/// the layer they operate on and iterate it directly. Owns no behaviour — it is the
/// data-oriented "world state" that tick systems mutate.
/// </summary>
public sealed class WorldMap
{
    private readonly Dictionary<MapLayer, IGridLayer> _layers = new();

    public int Width { get; }
    public int Height { get; }
    public IsometricProjector Projector { get; }

    // Strongly-typed handles to the layers every build sets up. Additional
    // heat-map layers (pollution, land value...) are registered on demand.
    public GridLayer<TerrainCell> Terrain { get; }
    public GridLayer<ZoneCell> Zoning { get; }

    public WorldMap(int width, int height, IsometricProjector? projector = null)
    {
        Width = width;
        Height = height;
        Projector = projector ?? new IsometricProjector();

        Terrain = AddLayer(new GridLayer<TerrainCell>(MapLayer.Terrain, width, height));
        Zoning = AddLayer(new GridLayer<ZoneCell>(MapLayer.Zoning, width, height));
    }

    /// <summary>Register a layer so it can be looked up by <see cref="MapLayer"/> id.</summary>
    public GridLayer<T> AddLayer<T>(GridLayer<T> layer) where T : struct
    {
        _layers[layer.Layer] = layer;
        return layer;
    }

    /// <summary>Retrieve a previously registered layer, typed.</summary>
    public GridLayer<T> GetLayer<T>(MapLayer layer) where T : struct
        => (GridLayer<T>)_layers[layer];

    public bool InBounds(GridCoord coord)
        => coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;

    public IEnumerable<IGridLayer> Layers => _layers.Values;
}
