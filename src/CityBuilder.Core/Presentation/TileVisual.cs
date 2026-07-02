namespace CityBuilder.Presentation;

/// <summary>How a placeholder tile should be drawn (colored primitive, no art assets yet).</summary>
public enum IsoShape : byte
{
    /// <summary>A flat ground diamond (terrain, zoning tint).</summary>
    FlatDiamond = 0,

    /// <summary>An extruded diamond prism (a building; height = bulk).</summary>
    Prism = 1,

    /// <summary>A small marker/billboard (vehicle, agent, icon).</summary>
    Marker = 2,
}

/// <summary>
/// A resolution-independent description of a placeholder visual: shape + colours + extrusion
/// height. The procedural sprite factory produces these from simulation data; the renderer
/// turns them into actual polygons. This is the "colored polygons mapped to isometric"
/// bootstrap art path.
/// </summary>
public readonly struct TileVisual
{
    public readonly IsoShape Shape;
    public readonly Color32 Fill;
    public readonly Color32 Outline;

    /// <summary>Extrusion height in elevation units (0 for flat ground).</summary>
    public readonly byte Height;

    public TileVisual(IsoShape shape, Color32 fill, Color32 outline, byte height = 0)
    {
        Shape = shape;
        Fill = fill;
        Outline = outline;
        Height = height;
    }
}
