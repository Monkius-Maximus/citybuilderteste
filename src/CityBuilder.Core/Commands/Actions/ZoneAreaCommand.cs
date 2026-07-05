using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Commands.Actions;

/// <summary>
/// Paint a rectangular area of the zoning layer with a zone type + density. Fully
/// reversible: it snapshots the affected cells on execute and restores them on undo.
/// </summary>
public sealed class ZoneAreaCommand : ICommand
{
    private readonly GridCoord _min;
    private readonly GridCoord _max;
    private readonly ZoneType _type;
    private readonly ZoneDensity _density;

    private ZoneCell[]? _previous; // snapshot for undo
    private int _spanX;

    public ZoneAreaCommand(GridCoord a, GridCoord b, ZoneType type, ZoneDensity density)
    {
        _min = new GridCoord(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        _max = new GridCoord(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        _type = type;
        _density = density;
    }

    // Read-only views of the captured parameters (used by the replay codec).
    public GridCoord Min => _min;
    public GridCoord Max => _max;
    public ZoneType Type => _type;
    public ZoneDensity Density => _density;

    public string Name => $"Zone({_type},{_density})";

    public bool CanExecute(ISimulationContext context)
        => _max.X >= 0 && _max.Y >= 0
           && _min.X < context.Map.Width && _min.Y < context.Map.Height
           && _type != ZoneType.None;

    public CommandResult Execute(ISimulationContext context)
    {
        GridLayer<ZoneCell> layer = context.Map.Zoning;

        int x0 = Math.Max(0, _min.X);
        int y0 = Math.Max(0, _min.Y);
        int x1 = Math.Min(context.Map.Width - 1, _max.X);
        int y1 = Math.Min(context.Map.Height - 1, _max.Y);

        _spanX = x1 - x0 + 1;
        int spanY = y1 - y0 + 1;
        _previous = new ZoneCell[_spanX * spanY];

        int painted = 0;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var coord = new GridCoord(x, y);
                _previous[(y - y0) * _spanX + (x - x0)] = layer[coord];

                // Re-zoning resets growth; the CA regrows it under the new rules.
                layer[coord] = new ZoneCell { Type = _type, Density = _density };
                painted++;
            }
        }

        return CommandResult.Succeeded($"Zoned {painted} cells.");
    }

    public void Undo(ISimulationContext context)
    {
        if (_previous is null)
        {
            return;
        }

        GridLayer<ZoneCell> layer = context.Map.Zoning;
        int x0 = Math.Max(0, _min.X);
        int y0 = Math.Max(0, _min.Y);

        for (int i = 0; i < _previous.Length; i++)
        {
            int x = x0 + (i % _spanX);
            int y = y0 + (i / _spanX);
            layer.TrySet(new GridCoord(x, y), _previous[i]);
        }
    }
}
