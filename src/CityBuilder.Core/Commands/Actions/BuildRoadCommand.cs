using CityBuilder.Grid;
using CityBuilder.Networks;

namespace CityBuilder.Commands.Actions;

/// <summary>
/// Lay a road segment between two cells: ensures both endpoints exist as nodes in the Road
/// network and connects them. Undo rolls the graph back to the checkpoint taken before the
/// edit (see <see cref="FlowNetwork.RestoreCheckpoint"/>).
/// </summary>
public sealed class BuildRoadCommand : ICommand
{
    private readonly GridCoord _from;
    private readonly GridCoord _to;
    private readonly int _capacity;

    private NetworkCheckpoint _checkpoint;

    public BuildRoadCommand(GridCoord from, GridCoord to, int capacity = 32)
    {
        _from = from;
        _to = to;
        _capacity = capacity;
    }

    // Read-only views of the captured parameters (used by the replay codec).
    public GridCoord From => _from;
    public GridCoord To => _to;
    public int Capacity => _capacity;

    public string Name => $"BuildRoad({_from}->{_to})";

    public bool CanExecute(ISimulationContext context)
        => context.Map.InBounds(_from)
           && context.Map.InBounds(_to)
           && _from != _to;

    public CommandResult Execute(ISimulationContext context)
    {
        FlowNetwork road = context.GetNetwork(NetworkType.Road);
        _checkpoint = road.CreateCheckpoint();

        NodeId a = road.AddNode(_from);
        NodeId b = road.AddNode(_to);
        float cost = GridCoord.ManhattanDistance(_from, _to);
        road.Connect(a, b, cost, _capacity, bidirectional: true);

        return CommandResult.Succeeded($"Road {_from}->{_to} built.");
    }

    public void Undo(ISimulationContext context)
        => context.GetNetwork(NetworkType.Road).RestoreCheckpoint(_checkpoint);
}
