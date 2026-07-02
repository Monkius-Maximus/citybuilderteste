using CityBuilder.Networks;

namespace CityBuilder.Pathfinding;

/// <summary>
/// A multi-source Dijkstra "flow field" (a.k.a. Dijkstra map). One pass from a set of goal
/// nodes computes, for EVERY node, the cost to the nearest goal and the next node to step
/// toward it. This replaces thousands of per-agent path queries: crowds just follow
/// <see cref="GetNext"/> downhill, and service coverage (fire/police/health reachability)
/// is read straight off <see cref="GetDistance"/>.
/// <para>
/// Following <see cref="GetNext"/> walks back toward a source, so the graph must be
/// traversable in that direction (roads/pedestrian nets are built bidirectional). For a
/// strictly directed network, build the map on its reverse graph.
/// </para>
/// </summary>
public sealed class DijkstraMap
{
    private readonly MinHeap _open = new();
    private float[] _distance = Array.Empty<float>();
    private int[] _next = Array.Empty<int>();
    private int _nodeCount;

    public int NodeCount => _nodeCount;

    /// <summary>Recompute the field from the given source nodes.</summary>
    public void Build(IPathGraph graph, IReadOnlyList<int> sources, IEdgeWeightProvider? weights = null)
    {
        int n = graph.NodeCount;
        EnsureCapacity(n);
        _nodeCount = n;

        Array.Fill(_distance, float.PositiveInfinity, 0, n);
        Array.Fill(_next, -1, 0, n);
        _open.Clear();

        for (int i = 0; i < sources.Count; i++)
        {
            int s = sources[i];
            if (s >= 0 && s < n)
            {
                _distance[s] = 0f;
                _open.Push(s, 0f);
            }
        }

        while (_open.TryPop(out int u, out float d))
        {
            if (d > _distance[u])
            {
                continue; // stale heap entry
            }

            IReadOnlyList<PathNeighbor> neighbors = graph.GetNeighbors(u);
            for (int i = 0; i < neighbors.Count; i++)
            {
                PathNeighbor edge = neighbors[i];
                float stepCost = weights?.GetWeight(edge.Edge, edge.BaseCost, edge.Capacity) ?? edge.BaseCost;
                float nd = _distance[u] + stepCost;

                if (nd < _distance[edge.Node])
                {
                    _distance[edge.Node] = nd;
                    _next[edge.Node] = u; // step from this node toward the nearest source
                    _open.Push(edge.Node, nd);
                }
            }
        }
    }

    /// <summary>Cost from <paramref name="node"/> to the nearest source (+∞ if unreachable).</summary>
    public float GetDistance(int node)
        => node >= 0 && node < _nodeCount ? _distance[node] : float.PositiveInfinity;

    public bool IsReachable(int node)
        => node >= 0 && node < _nodeCount && !float.IsPositiveInfinity(_distance[node]);

    /// <summary>Next node to move to in order to approach the nearest source (-1 if none / already at a source).</summary>
    public int GetNext(int node)
        => node >= 0 && node < _nodeCount ? _next[node] : -1;

    private void EnsureCapacity(int n)
    {
        if (_distance.Length >= n)
        {
            return;
        }

        int size = Math.Max(16, _distance.Length);
        while (size < n)
        {
            size *= 2;
        }

        _distance = new float[size];
        _next = new int[size];
    }
}
