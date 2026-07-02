using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Commands.Actions;

/// <summary>
/// Clear the zoning designation (and thus development) from a rectangle. Snapshots the
/// affected cells for a clean undo and emits a demolition notification.
/// </summary>
public sealed class BulldozeAreaCommand : ICommand
{
    private readonly GridCoord _min;
    private readonly GridCoord _max;

    private ZoneCell[]? _previous;
    private int _spanX;

    public BulldozeAreaCommand(GridCoord a, GridCoord b)
    {
        _min = new GridCoord(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        _max = new GridCoord(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    public string Name => $"Bulldoze({_min}..{_max})";

    public bool CanExecute(ISimulationContext context)
        => _max.X >= 0 && _max.Y >= 0
           && _min.X < context.Map.Width && _min.Y < context.Map.Height;

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

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var coord = new GridCoord(x, y);
                _previous[(y - y0) * _spanX + (x - x0)] = layer[coord];
                layer[coord] = default; // ZoneType.None, no development
                context.Events.Publish(new StructureDemolishedEvent(coord));
            }
        }

        return CommandResult.Succeeded($"Cleared {_previous.Length} cells.");
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
