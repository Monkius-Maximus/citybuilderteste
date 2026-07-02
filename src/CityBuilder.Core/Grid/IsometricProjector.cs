namespace CityBuilder.Grid;

/// <summary>
/// Converts between logical grid coordinates and 2:1 "dimetric" screen space
/// (the classic SimCity/RollerCoaster look). Pure math, no engine types, so it is
/// fully unit-testable in a console.
/// <para>
/// Screen layout (diamond tiles): moving +X on the grid goes down-right on screen,
/// moving +Y goes down-left. Elevation shifts a tile straight up by
/// <see cref="ElevationStep"/> pixels per height unit.
/// </para>
/// </summary>
public sealed class IsometricProjector
{
    /// <summary>Full width of a tile diamond in pixels.</summary>
    public float TileWidth { get; }

    /// <summary>Full height of a tile diamond in pixels (typically TileWidth / 2 for 2:1 iso).</summary>
    public float TileHeight { get; }

    /// <summary>Vertical pixel offset applied per unit of terrain elevation.</summary>
    public float ElevationStep { get; }

    private readonly float _halfWidth;
    private readonly float _halfHeight;

    public IsometricProjector(float tileWidth = 64f, float tileHeight = 32f, float elevationStep = 16f)
    {
        if (tileWidth <= 0f || tileHeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(tileWidth), "Tile dimensions must be positive.");
        }

        TileWidth = tileWidth;
        TileHeight = tileHeight;
        ElevationStep = elevationStep;
        _halfWidth = tileWidth * 0.5f;
        _halfHeight = tileHeight * 0.5f;
    }

    /// <summary>Project a grid cell (with optional elevation) to the centre of its screen diamond.</summary>
    public ScreenPoint GridToScreen(GridCoord coord, int elevation = 0)
        => GridToScreen(coord.X, coord.Y, elevation);

    public ScreenPoint GridToScreen(float gx, float gy, int elevation = 0)
    {
        float sx = (gx - gy) * _halfWidth;
        float sy = (gx + gy) * _halfHeight - elevation * ElevationStep;
        return new ScreenPoint(sx, sy);
    }

    /// <summary>
    /// Inverse projection: turn a screen point back into a (fractional) grid position,
    /// ignoring elevation. The caller floors the result to hit-test a specific cell.
    /// </summary>
    public ScreenPoint ScreenToGrid(ScreenPoint screen)
    {
        float gx = (screen.X / _halfWidth + screen.Y / _halfHeight) * 0.5f;
        float gy = (screen.Y / _halfHeight - screen.X / _halfWidth) * 0.5f;
        return new ScreenPoint(gx, gy);
    }

    /// <summary>Snap a raw screen point to the integer grid cell under it (elevation ignored).</summary>
    public GridCoord ScreenToCell(ScreenPoint screen)
    {
        ScreenPoint g = ScreenToGrid(screen);
        return new GridCoord((int)MathF.Floor(g.X + 0.5f), (int)MathF.Floor(g.Y + 0.5f));
    }

    /// <summary>
    /// Painter's-algorithm sort key. Tiles with a larger key are drawn later
    /// (in front). Guarantees correct back-to-front overlap for isometric tiles.
    /// </summary>
    public long DepthKey(GridCoord coord, int elevation = 0, MapLayer layer = MapLayer.Surface)
        => ((long)(coord.X + coord.Y) << 20) | ((long)elevation << 8) | (byte)layer;
}
