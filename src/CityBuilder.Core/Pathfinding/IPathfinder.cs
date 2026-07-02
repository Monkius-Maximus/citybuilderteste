using CityBuilder.Networks;

namespace CityBuilder.Pathfinding;

/// <summary>
/// Point-to-point path query over an <see cref="IPathGraph"/>. The resulting node
/// sequence is written into <paramref name="pathBuffer"/> (cleared first) so callers can
/// reuse a pooled list. Pass an <see cref="IEdgeWeightProvider"/> to route with dynamic
/// costs (e.g. congestion); pass null to use static base costs.
/// </summary>
public interface IPathfinder
{
    PathResult FindPath(
        IPathGraph graph,
        int start,
        int goal,
        List<int> pathBuffer,
        IEdgeWeightProvider? weights = null);
}
