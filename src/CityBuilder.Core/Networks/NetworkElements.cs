using CityBuilder.Grid;

namespace CityBuilder.Networks;

/// <summary>A vertex in a transport/utility graph, anchored to a map cell.</summary>
public readonly struct NetworkNode
{
    public readonly NodeId Id;
    public readonly GridCoord Coord;

    public NetworkNode(NodeId id, GridCoord coord)
    {
        Id = id;
        Coord = coord;
    }
}

/// <summary>
/// A directed connection between two nodes. <see cref="BaseCost"/> is the static traversal
/// cost (distance / build quality); <see cref="Capacity"/> feeds dynamic congestion models
/// that scale the effective weight at pathfinding time.
/// </summary>
public readonly struct NetworkEdge
{
    public readonly EdgeId Id;
    public readonly NodeId From;
    public readonly NodeId To;
    public readonly float BaseCost;
    public readonly int Capacity;

    public NetworkEdge(EdgeId id, NodeId from, NodeId to, float baseCost, int capacity)
    {
        Id = id;
        From = from;
        To = to;
        BaseCost = baseCost;
        Capacity = capacity;
    }
}

/// <summary>Opaque snapshot of a network's size, used to roll back appended nodes/edges.</summary>
public readonly struct NetworkCheckpoint
{
    public readonly int NodeCount;
    public readonly int EdgeCount;

    public NetworkCheckpoint(int nodeCount, int edgeCount)
    {
        NodeCount = nodeCount;
        EdgeCount = edgeCount;
    }
}
