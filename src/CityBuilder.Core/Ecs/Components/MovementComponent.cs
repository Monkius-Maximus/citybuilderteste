using CityBuilder.Networks;

namespace CityBuilder.Ecs.Components;

/// <summary>
/// Movement state for a mobile agent (vehicle/pedestrian) traversing the transport graph.
/// The agent follows a route (a list of node ids managed by the traffic layer); this
/// component caches just the current edge so the per-tick integration stays branch-light and
/// allocation-free. Screen position is interpolated between the From/To node coords by
/// <see cref="Progress"/>.
/// </summary>
public struct MovementComponent : IComponent
{
    /// <summary>Road node the agent is leaving.</summary>
    public int FromNode;

    /// <summary>Road node the agent is heading to.</summary>
    public int ToNode;

    /// <summary>Edge being traversed (used to attribute congestion load).</summary>
    public EdgeId Edge;

    /// <summary>Length of the current edge in cells (its base cost); used to normalise progress.</summary>
    public float EdgeLength;

    /// <summary>Interpolation 0..1 from <see cref="FromNode"/> to <see cref="ToNode"/>.</summary>
    public float Progress;

    /// <summary>Cells per second at free flow.</summary>
    public float Speed;

    /// <summary>Index of <see cref="FromNode"/> within the agent's route buffer.</summary>
    public int PathCursor;

    /// <summary>False when the agent has no active route (idle / awaiting despawn).</summary>
    public bool Active;
}
