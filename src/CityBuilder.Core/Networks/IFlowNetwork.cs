using CityBuilder.Grid;

namespace CityBuilder.Networks;

/// <summary>
/// Mutable contract for a transport/utility graph: build it up by adding nodes and
/// connecting them, and query topology. Distinct from the read-only <c>IPathGraph</c>
/// consumed by the pathfinders — construction and traversal are separate concerns.
/// </summary>
public interface IFlowNetwork
{
    NetworkType Type { get; }
    int NodeCount { get; }
    int EdgeCount { get; }

    /// <summary>Add (or fetch the existing) node at a cell.</summary>
    NodeId AddNode(GridCoord coord);

    /// <summary>Connect two nodes. When bidirectional, inserts the reverse edge too.</summary>
    EdgeId Connect(NodeId from, NodeId to, float baseCost, int capacity, bool bidirectional = true);

    bool TryGetNodeAt(GridCoord coord, out NodeId node);

    NetworkNode GetNode(NodeId id);

    NetworkEdge GetEdge(EdgeId id);
}
