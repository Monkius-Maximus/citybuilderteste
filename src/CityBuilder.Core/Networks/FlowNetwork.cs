using CityBuilder.Grid;
using CityBuilder.Pathfinding;

namespace CityBuilder.Networks;

/// <summary>
/// Adjacency-list graph for one <see cref="NetworkType"/>. Nodes get dense 0-based ids so
/// they double as array indices for the pathfinders. Implements <see cref="IPathGraph"/>
/// directly, so A*/Dijkstra/flow-fields run on it without any adapter.
/// </summary>
public sealed class FlowNetwork : IFlowNetwork, IPathGraph
{
    private readonly List<NetworkNode> _nodes = new();
    private readonly List<NetworkEdge> _edges = new();
    private readonly List<List<PathNeighbor>> _adjacency = new();
    private readonly Dictionary<GridCoord, NodeId> _nodeAt = new();

    public FlowNetwork(NetworkType type) => Type = type;

    public NetworkType Type { get; }
    public int NodeCount => _nodes.Count;
    public int EdgeCount => _edges.Count;

    public NodeId AddNode(GridCoord coord)
    {
        if (_nodeAt.TryGetValue(coord, out NodeId existing))
        {
            return existing;
        }

        var id = new NodeId(_nodes.Count);
        _nodes.Add(new NetworkNode(id, coord));
        _adjacency.Add(new List<PathNeighbor>(4));
        _nodeAt[coord] = id;
        return id;
    }

    public EdgeId Connect(NodeId from, NodeId to, float baseCost, int capacity, bool bidirectional = true)
    {
        if (!IsValid(from) || !IsValid(to))
        {
            throw new ArgumentException("Cannot connect nodes that do not belong to this network.");
        }

        EdgeId id = AddDirectedEdge(from, to, baseCost, capacity);
        if (bidirectional)
        {
            AddDirectedEdge(to, from, baseCost, capacity);
        }

        return id;
    }

    private EdgeId AddDirectedEdge(NodeId from, NodeId to, float baseCost, int capacity)
    {
        var id = new EdgeId(_edges.Count);
        _edges.Add(new NetworkEdge(id, from, to, baseCost, capacity));
        _adjacency[from.Value].Add(new PathNeighbor(to.Value, id, baseCost, capacity));
        return id;
    }

    public bool TryGetNodeAt(GridCoord coord, out NodeId node) => _nodeAt.TryGetValue(coord, out node);

    public NetworkNode GetNode(NodeId id) => _nodes[id.Value];

    public NetworkEdge GetEdge(EdgeId id) => _edges[id.Value];

    private bool IsValid(NodeId id) => id.Value >= 0 && id.Value < _nodes.Count;

    // --- Checkpoint / restore ---
    // The graph is append-only, so a snapshot of the node/edge counts is enough to roll
    // back the most-recent additions. This gives commands a cheap, correct Undo for LIFO
    // history (the only network mutations are appends, undone newest-first). Full structural
    // editing (recycling arbitrary interior nodes) is a planned follow-up.

    public NetworkCheckpoint CreateCheckpoint() => new(_nodes.Count, _edges.Count);

    /// <summary>Roll the graph back to a checkpoint, dropping nodes/edges added afterwards.</summary>
    public void RestoreCheckpoint(NetworkCheckpoint checkpoint)
    {
        int keepNodes = checkpoint.NodeCount;
        int keepEdges = checkpoint.EdgeCount;

        if (keepNodes > _nodes.Count || keepEdges > _edges.Count)
        {
            throw new InvalidOperationException("Checkpoint is newer than the current network state.");
        }

        // Forget the coordinates of nodes being removed.
        for (int i = keepNodes; i < _nodes.Count; i++)
        {
            _nodeAt.Remove(_nodes[i].Coord);
        }

        _nodes.RemoveRange(keepNodes, _nodes.Count - keepNodes);
        _adjacency.RemoveRange(keepNodes, _adjacency.Count - keepNodes);
        _edges.RemoveRange(keepEdges, _edges.Count - keepEdges);

        // Purge dangling neighbours that referenced removed edges/nodes from surviving nodes.
        for (int node = 0; node < _adjacency.Count; node++)
        {
            List<PathNeighbor> list = _adjacency[node];
            for (int k = list.Count - 1; k >= 0; k--)
            {
                if (list[k].Edge.Value >= keepEdges || list[k].Node >= keepNodes)
                {
                    list.RemoveAt(k);
                }
            }
        }
    }

    // --- IPathGraph ---

    public GridCoord GetCoord(int node) => _nodes[node].Coord;

    public IReadOnlyList<PathNeighbor> GetNeighbors(int node) => _adjacency[node];
}
