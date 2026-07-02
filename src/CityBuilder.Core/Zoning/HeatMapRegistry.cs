using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>Owns the set of active <see cref="HeatMap"/>s and serves them by kind.</summary>
public sealed class HeatMapRegistry : IHeatMapProvider
{
    private readonly Dictionary<HeatMapKind, HeatMap> _maps = new();
    private readonly int _width;
    private readonly int _height;

    public HeatMapRegistry(int width, int height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>Create (or fetch) the field for a given kind.</summary>
    public HeatMap GetOrCreate(HeatMapKind kind)
    {
        if (!_maps.TryGetValue(kind, out HeatMap? map))
        {
            map = new HeatMap(kind, _width, _height);
            _maps[kind] = map;
        }

        return map;
    }

    public bool TryGet(HeatMapKind kind, out HeatMap map)
    {
        if (_maps.TryGetValue(kind, out HeatMap? found))
        {
            map = found;
            return true;
        }

        map = null!;
        return false;
    }

    public float Sample(HeatMapKind kind, GridCoord cell)
        => _maps.TryGetValue(kind, out HeatMap? map) ? map.Sample(cell) : 0f;

    public IEnumerable<HeatMap> All => _maps.Values;
}
