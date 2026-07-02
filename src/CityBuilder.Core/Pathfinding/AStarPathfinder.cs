using CityBuilder.Grid;
using CityBuilder.Networks;

namespace CityBuilder.Pathfinding;

/// <summary>
/// A* over an <see cref="IPathGraph"/> with pluggable heuristic and dynamic edge weights.
/// <para>
/// Working buffers (g-scores, came-from links, closed set) are retained between calls and
/// invalidated with a per-search "stamp" instead of being re-cleared — so repeated queries
/// on a large map allocate nothing and don't pay an O(N) reset each time.
/// </para>
/// </summary>
public sealed class AStarPathfinder : IPathfinder
{
    private readonly IHeuristic _heuristic;

    /// <summary>
    /// Multiplier on the heuristic. 1 = optimal A*; &gt;1 = "weighted A*" that finds a route
    /// faster at the cost of optimality (handy for far-off, low-priority agents).
    /// </summary>
    private readonly float _heuristicWeight;

    private readonly MinHeap _open = new();

    private float[] _g = Array.Empty<float>();
    private int[] _cameFrom = Array.Empty<int>();
    private int[] _gStamp = Array.Empty<int>();
    private int[] _closedStamp = Array.Empty<int>();
    private int _stamp;

    public AStarPathfinder(IHeuristic? heuristic = null, float heuristicWeight = 1.0f)
    {
        _heuristic = heuristic ?? Heuristics.Manhattan;
        _heuristicWeight = heuristicWeight;
    }

    public PathResult FindPath(
        IPathGraph graph,
        int start,
        int goal,
        List<int> pathBuffer,
        IEdgeWeightProvider? weights = null)
    {
        pathBuffer.Clear();

        int n = graph.NodeCount;
        if (start < 0 || goal < 0 || start >= n || goal >= n)
        {
            return PathResult.NotFound;
        }

        EnsureCapacity(n);
        _stamp++;
        _open.Clear();

        GridCoord goalCoord = graph.GetCoord(goal);

        SetG(start, 0f);
        _cameFrom[start] = -1;
        _open.Push(start, _heuristic.Estimate(graph.GetCoord(start), goalCoord) * _heuristicWeight);

        while (_open.TryPop(out int current, out _))
        {
            if (current == goal)
            {
                return Reconstruct(current, pathBuffer);
            }

            if (IsClosed(current))
            {
                continue; // stale duplicate — already finalised
            }

            SetClosed(current);
            float currentG = GetG(current);

            IReadOnlyList<PathNeighbor> neighbors = graph.GetNeighbors(current);
            for (int i = 0; i < neighbors.Count; i++)
            {
                PathNeighbor edge = neighbors[i];
                if (IsClosed(edge.Node))
                {
                    continue;
                }

                float stepCost = weights?.GetWeight(edge.Edge, edge.BaseCost, edge.Capacity) ?? edge.BaseCost;
                float tentativeG = currentG + stepCost;

                if (tentativeG < GetG(edge.Node))
                {
                    SetG(edge.Node, tentativeG);
                    _cameFrom[edge.Node] = current;
                    float f = tentativeG + _heuristic.Estimate(graph.GetCoord(edge.Node), goalCoord) * _heuristicWeight;
                    _open.Push(edge.Node, f);
                }
            }
        }

        return PathResult.NotFound;
    }

    private PathResult Reconstruct(int goal, List<int> pathBuffer)
    {
        float cost = GetG(goal);

        for (int node = goal; node != -1; node = _cameFrom[node])
        {
            pathBuffer.Add(node);
        }

        pathBuffer.Reverse(); // was goal→start, now start→goal
        return new PathResult(true, cost, pathBuffer.Count);
    }

    private void EnsureCapacity(int n)
    {
        if (_g.Length >= n)
        {
            return;
        }

        int size = Math.Max(16, _g.Length);
        while (size < n)
        {
            size *= 2;
        }

        Array.Resize(ref _g, size);
        Array.Resize(ref _cameFrom, size);
        Array.Resize(ref _gStamp, size);
        Array.Resize(ref _closedStamp, size);
    }

    // g-score is "infinity" unless it was written during the current search (stamp match).
    private float GetG(int node) => _gStamp[node] == _stamp ? _g[node] : float.PositiveInfinity;

    private void SetG(int node, float value)
    {
        _g[node] = value;
        _gStamp[node] = _stamp;
    }

    private bool IsClosed(int node) => _closedStamp[node] == _stamp;

    private void SetClosed(int node) => _closedStamp[node] = _stamp;
}
