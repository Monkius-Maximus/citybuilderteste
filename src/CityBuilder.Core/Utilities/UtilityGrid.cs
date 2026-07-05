using CityBuilder.Networks;
using CityBuilder.Pathfinding;

namespace CityBuilder.Utilities;

/// <summary>
/// One utility service (power OR water) over its <see cref="FlowNetwork"/>. Coverage is a
/// multi-source <see cref="DijkstraMap"/> from the supply nodes: every node learns its cost to
/// the nearest source in a single pass, so "is this consumer connected and in range?" is O(1) —
/// no per-consumer path query. Capacity is then allocated nearest-first, producing realistic
/// brownouts when demand outstrips supply. Deterministic (no RNG; stable distance ordering).
/// </summary>
public sealed class UtilityGrid
{
    private readonly FlowNetwork _network;
    private readonly List<UtilitySource> _sources = new();
    private readonly List<UtilityConsumer> _consumers = new();
    private readonly DijkstraMap _coverage = new();
    private readonly List<int> _sourceNodes = new();
    private readonly List<int> _order = new();
    private readonly Comparison<int> _byCoverage;

    public UtilityGrid(NetworkType kind, FlowNetwork network)
    {
        Kind = kind;
        _network = network;
        _byCoverage = CompareByCoverage;
    }

    public NetworkType Kind { get; }

    /// <summary>Maximum coverage cost from a source for a consumer to count as connected.</summary>
    public float MaxServiceDistance { get; set; } = float.PositiveInfinity;

    public FlowNetwork Network => _network;
    public IReadOnlyList<UtilitySource> Sources => _sources;
    public IReadOnlyList<UtilityConsumer> Consumers => _consumers;

    public void AddSource(NodeId node, float capacity) => _sources.Add(new UtilitySource(node, capacity));

    public int AddConsumer(NodeId node, float demand)
    {
        _consumers.Add(new UtilityConsumer { Node = node, Demand = demand, Served = false });
        return _consumers.Count - 1;
    }

    public void ClearSources() => _sources.Clear();
    public void ClearConsumers() => _consumers.Clear();

    /// <summary>Coverage cost from the nearest source to a node (+∞ if unreachable).</summary>
    public float CoverageDistance(NodeId node) => _coverage.GetDistance(node.Value);

    /// <summary>Recompute coverage + supply allocation. Returns the network-wide report.</summary>
    public UtilityReport Solve()
    {
        // 1) Flow field from every supply node in one Dijkstra pass.
        _sourceNodes.Clear();
        float totalSupply = 0f;
        for (int i = 0; i < _sources.Count; i++)
        {
            _sourceNodes.Add(_sources[i].Node.Value);
            totalSupply += _sources[i].Capacity;
        }

        _coverage.Build(_network, _sourceNodes);

        // 2) Find in-range consumers, reset their served flag, tally demand.
        _order.Clear();
        float totalDemand = 0f;
        float reachableDemand = 0f;
        for (int i = 0; i < _consumers.Count; i++)
        {
            UtilityConsumer c = _consumers[i];
            c.Served = false;
            _consumers[i] = c;
            totalDemand += c.Demand;

            float dist = _coverage.GetDistance(c.Node.Value);
            if (!float.IsPositiveInfinity(dist) && dist <= MaxServiceDistance)
            {
                _order.Add(i);
                reachableDemand += c.Demand;
            }
        }

        // 3) Allocate supply nearest-first (a farther small load may still fit after a skip).
        _order.Sort(_byCoverage);

        float remaining = totalSupply;
        float servedDemand = 0f;
        int servedConsumers = 0;
        for (int k = 0; k < _order.Count; k++)
        {
            int idx = _order[k];
            UtilityConsumer c = _consumers[idx];
            if (c.Demand <= remaining + 1e-4f)
            {
                c.Served = true;
                _consumers[idx] = c;
                remaining -= c.Demand;
                servedDemand += c.Demand;
                servedConsumers++;
            }
        }

        bool brownout = reachableDemand > totalSupply + 1e-4f;
        return new UtilityReport(Kind, totalSupply, totalDemand, reachableDemand, servedDemand, servedConsumers, _order.Count, brownout);
    }

    private int CompareByCoverage(int x, int y)
    {
        float dx = _coverage.GetDistance(_consumers[x].Node.Value);
        float dy = _coverage.GetDistance(_consumers[y].Node.Value);
        int cmp = dx.CompareTo(dy);
        return cmp != 0 ? cmp : x.CompareTo(y); // stable tie-break keeps the solve deterministic
    }
}
