namespace CityBuilder.Grid;

/// <summary>
/// The stacked data planes that share one X/Y footprint. Each layer stores a
/// different aspect of a tile; systems read/write the layers they own. Splitting
/// data this way (Structure-of-Arrays across layers) keeps each simulation pass
/// scanning a single tight array instead of a fat "Tile" object.
/// </summary>
public enum MapLayer : byte
{
    /// <summary>Elevation, water, terrain type. Read by almost everything.</summary>
    Terrain = 0,

    /// <summary>Subways, water mains, sewage, buried power — the underground plane.</summary>
    Underground = 1,

    /// <summary>Roads, rails, tram lines, sidewalks laid on the ground.</summary>
    Surface = 2,

    /// <summary>Zone designation + growth state (residential/commercial/industrial).</summary>
    Zoning = 3,

    /// <summary>Placed buildings and props occupying a tile.</summary>
    Structures = 4,

    /// <summary>Transient overlays for UI/debug heat-maps (pollution, land value...).</summary>
    Overlay = 5,
}
