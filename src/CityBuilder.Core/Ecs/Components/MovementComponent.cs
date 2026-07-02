using CityBuilder.Grid;

namespace CityBuilder.Ecs.Components;

/// <summary>
/// Movement state for a mobile agent (vehicle/pedestrian). The agent walks a path
/// produced by the pathfinding layer; a movement system advances <see cref="Progress"/>
/// toward the next node each tick. Example component only.
/// </summary>
public struct MovementComponent : IComponent
{
    public GridCoord Origin;
    public GridCoord Destination;

    /// <summary>Interpolation 0..1 between the current path node and the next.</summary>
    public float Progress;

    /// <summary>Cells per second the agent moves (scaled by tick delta).</summary>
    public float Speed;

    /// <summary>Index into the agent's current path buffer.</summary>
    public int PathCursor;
}
