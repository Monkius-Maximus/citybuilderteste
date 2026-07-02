using CityBuilder.Grid;

namespace CityBuilder.Pathfinding;

/// <summary>
/// The minimal read-only view a pathfinder needs of any graph. Nodes are dense integers
/// in [0, NodeCount). Both the transport <c>FlowNetwork</c> and a raw tile grid can
/// implement this, so A*/Dijkstra work uniformly over roads, rails or walkable terrain.
/// </summary>
public interface IPathGraph
{
    int NodeCount { get; }

    /// <summary>Map a node to its map cell — used by heuristics for distance estimates.</summary>
    GridCoord GetCoord(int node);

    /// <summary>Outgoing neighbours of a node. Implementations should return a cached list (no per-call allocation).</summary>
    IReadOnlyList<PathNeighbor> GetNeighbors(int node);
}
