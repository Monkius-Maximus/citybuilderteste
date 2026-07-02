using CityBuilder.Networks;

namespace CityBuilder.Pathfinding;

/// <summary>
/// One outgoing connection from a node, as seen by a pathfinder: the destination node,
/// the edge taken (so dynamic weights can be looked up), its static base cost and the
/// edge capacity (for congestion models). Carrying these primitives keeps the pathfinder
/// independent of the concrete <c>NetworkEdge</c> layout.
/// </summary>
public readonly struct PathNeighbor
{
    public readonly int Node;
    public readonly EdgeId Edge;
    public readonly float BaseCost;
    public readonly int Capacity;

    public PathNeighbor(int node, EdgeId edge, float baseCost, int capacity)
    {
        Node = node;
        Edge = edge;
        BaseCost = baseCost;
        Capacity = capacity;
    }
}
