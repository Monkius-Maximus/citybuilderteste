namespace CityBuilder.Networks;

/// <summary>
/// Supplies the DYNAMIC traversal cost of an edge at query time. This is the hook that
/// lets congestion, tolls, weather or damage change routing without rebuilding the graph:
/// A*/Dijkstra call it per edge instead of trusting the static base cost. It intentionally
/// takes only primitives (edge id / base cost / capacity) so the pathfinding layer stays
/// decoupled from the concrete graph structs.
/// </summary>
public interface IEdgeWeightProvider
{
    /// <summary>Effective cost of traversing an edge right now. Must be ≥ 0 for A* admissibility.</summary>
    float GetWeight(EdgeId edge, float baseCost, int capacity);
}

/// <summary>Trivial provider that returns the static base cost (no dynamics).</summary>
public sealed class StaticEdgeWeightProvider : IEdgeWeightProvider
{
    public static readonly StaticEdgeWeightProvider Instance = new();

    public float GetWeight(EdgeId edge, float baseCost, int capacity) => baseCost;
}

/// <summary>
/// Example dynamic model: effective cost rises with live load relative to capacity, so
/// pathfinders naturally route around jams. Load is updated by the traffic system each tick.
/// </summary>
public sealed class CongestionWeightProvider : IEdgeWeightProvider
{
    private float[] _load;
    private readonly float _sensitivity;

    public CongestionWeightProvider(int edgeCapacityHint = 64, float sensitivity = 2.0f)
    {
        _load = new float[Math.Max(1, edgeCapacityHint)];
        _sensitivity = sensitivity;
    }

    /// <summary>Set the current number of agents on an edge (called by the traffic system).</summary>
    public void SetLoad(EdgeId edge, float load)
    {
        EnsureCapacity(edge.Value);
        _load[edge.Value] = load;
    }

    public float GetWeight(EdgeId edge, float baseCost, int capacity)
    {
        if (capacity <= 0 || edge.Value < 0 || edge.Value >= _load.Length)
        {
            return baseCost;
        }

        float ratio = _load[edge.Value] / capacity;
        // Bureau of Public Roads-style volume/delay curve (simplified, deterministic).
        return baseCost * (1f + _sensitivity * ratio * ratio);
    }

    private void EnsureCapacity(int index)
    {
        if (index < _load.Length)
        {
            return;
        }

        int size = _load.Length;
        while (size <= index)
        {
            size *= 2;
        }

        Array.Resize(ref _load, size);
    }
}
