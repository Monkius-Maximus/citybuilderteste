namespace CityBuilder.Grid;

/// <summary>
/// A dense, cache-friendly 2D field of value-type cells backed by a single flat
/// array (row-major). Because <typeparamref name="T"/> is a <c>struct</c>, the whole
/// layer is one contiguous block of memory — ideal for the tight per-tile scans the
/// zoning/heat-map/CA systems perform every tick.
/// </summary>
/// <typeparam name="T">Value type stored per cell (e.g. <c>ZoneCell</c>, <c>float</c>, <c>TerrainCell</c>).</typeparam>
public sealed class GridLayer<T> : IGridLayer where T : struct
{
    private readonly T[] _cells;

    public MapLayer Layer { get; }
    public int Width { get; }
    public int Height { get; }

    public GridLayer(MapLayer layer, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
        }

        Layer = layer;
        Width = width;
        Height = height;
        _cells = new T[width * height];
    }

    /// <summary>Direct access to the backing store for vectorised / span-based passes.</summary>
    public Span<T> AsSpan() => _cells.AsSpan();

    public int ToIndex(GridCoord coord) => coord.Y * Width + coord.X;

    public GridCoord FromIndex(int index) => new(index % Width, index / Width);

    public bool InBounds(GridCoord coord)
        => coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;

    /// <summary>Indexer with bounds checking via the array itself.</summary>
    public ref T this[GridCoord coord] => ref _cells[coord.Y * Width + coord.X];

    public ref T this[int x, int y] => ref _cells[y * Width + x];

    /// <summary>Safe read that returns <paramref name="fallback"/> for out-of-bounds cells.</summary>
    public T GetOrDefault(GridCoord coord, T fallback = default)
        => InBounds(coord) ? _cells[coord.Y * Width + coord.X] : fallback;

    /// <summary>Safe write that silently ignores out-of-bounds cells.</summary>
    public bool TrySet(GridCoord coord, in T value)
    {
        if (!InBounds(coord))
        {
            return false;
        }

        _cells[coord.Y * Width + coord.X] = value;
        return true;
    }

    public void Clear() => Array.Clear(_cells, 0, _cells.Length);

    /// <summary>Fill every cell with a single value.</summary>
    public void Fill(T value)
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i] = value;
        }
    }
}
