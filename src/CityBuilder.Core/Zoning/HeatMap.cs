using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>
/// Semantic classification of a scalar field. These are the "pressures" the cellular
/// automata read to decide growth: high desirability & land value grow zones; high
/// pollution & crime decay them.
/// </summary>
public enum HeatMapKind : byte
{
    Desirability = 0,
    LandValue = 1,
    Pollution = 2,
    Crime = 3,
    TrafficNoise = 4,

    /// <summary>Coverage of an essential service (fire/police/health) — see Dijkstra flow fields.</summary>
    ServiceCoverage = 5,
}

/// <summary>
/// A scalar field over the map (one float per tile) with the diffusion + decay
/// operators heat-maps need. Pollution spreads and fades; desirability blends with
/// its surroundings. Backed by a <see cref="GridLayer{T}"/> so it can double as an
/// <see cref="MapLayer.Overlay"/> for the renderer.
/// </summary>
public sealed class HeatMap
{
    private readonly GridLayer<float> _field;
    private float[] _scratch;

    public HeatMapKind Kind { get; }
    public int Width => _field.Width;
    public int Height => _field.Height;
    public GridLayer<float> Field => _field;

    public HeatMap(HeatMapKind kind, int width, int height)
    {
        Kind = kind;
        _field = new GridLayer<float>(MapLayer.Overlay, width, height);
        _scratch = new float[width * height];
    }

    public float Sample(GridCoord cell) => _field.GetOrDefault(cell);

    public void AddSource(GridCoord cell, float amount)
    {
        if (_field.InBounds(cell))
        {
            _field[cell] += amount;
        }
    }

    public void Clear() => _field.Clear();

    /// <summary>Multiplicative fade toward zero (e.g. pollution dissipating over time).</summary>
    public void Decay(float retention)
    {
        Span<float> cells = _field.AsSpan();
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] *= retention;
        }
    }

    /// <summary>
    /// One box-blur diffusion pass: each cell relaxes toward the average of its 4-neighbours.
    /// <paramref name="rate"/> in [0,1] controls how fast the field spreads.
    /// </summary>
    public void Diffuse(float rate)
    {
        int w = Width;
        int h = Height;
        Span<float> src = _field.AsSpan();

        if (_scratch.Length != src.Length)
        {
            _scratch = new float[src.Length];
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float center = src[i];
                float sum = 0f;
                int n = 0;

                if (x > 0) { sum += src[i - 1]; n++; }
                if (x < w - 1) { sum += src[i + 1]; n++; }
                if (y > 0) { sum += src[i - w]; n++; }
                if (y < h - 1) { sum += src[i + w]; n++; }

                float average = n > 0 ? sum / n : center;
                _scratch[i] = center + (average - center) * rate;
            }
        }

        _scratch.AsSpan(0, src.Length).CopyTo(src);
    }
}
