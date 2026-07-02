using CityBuilder.Networks;

namespace CityBuilder.Pathfinding;

/// <summary>
/// Uniform-cost shortest path. Dijkstra is exactly A* with a zero heuristic, so this
/// composes an <see cref="AStarPathfinder"/> configured with <see cref="Heuristics.Zero"/>
/// rather than duplicating the search loop. Use it when no admissible heuristic exists
/// (abstract utility graphs) or when you need guaranteed-optimal exploration order.
/// </summary>
public sealed class DijkstraPathfinder : IPathfinder
{
    private readonly AStarPathfinder _inner = new(Heuristics.Zero);

    public PathResult FindPath(
        IPathGraph graph,
        int start,
        int goal,
        List<int> pathBuffer,
        IEdgeWeightProvider? weights = null)
        => _inner.FindPath(graph, start, goal, pathBuffer, weights);
}
