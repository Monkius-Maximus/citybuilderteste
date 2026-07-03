using CityBuilder.Networks;

namespace CityBuilder.Utilities;

/// <summary>A supply point on a utility network (power plant, water pump): a node + capacity.</summary>
public readonly struct UtilitySource
{
    public readonly NodeId Node;
    public readonly float Capacity;

    public UtilitySource(NodeId node, float capacity)
    {
        Node = node;
        Capacity = capacity;
    }
}

/// <summary>A demand point on a utility network (a building/zone). <see cref="Served"/> is filled by the grid solve.</summary>
public struct UtilityConsumer
{
    public NodeId Node;
    public float Demand;
    public bool Served;
}

/// <summary>Network-wide result of one utility solve — what the UI/economy reads.</summary>
public readonly struct UtilityReport
{
    public readonly NetworkType Kind;
    public readonly float TotalSupply;
    public readonly float TotalDemand;

    /// <summary>Demand of consumers actually connected/in range of a source.</summary>
    public readonly float ReachableDemand;

    /// <summary>Demand that was actually powered given the available supply.</summary>
    public readonly float ServedDemand;

    public readonly int ServedConsumers;
    public readonly int ReachableConsumers;

    /// <summary>True when reachable demand exceeds supply (some in-range consumers go dark).</summary>
    public readonly bool Brownout;

    public UtilityReport(
        NetworkType kind,
        float totalSupply,
        float totalDemand,
        float reachableDemand,
        float servedDemand,
        int servedConsumers,
        int reachableConsumers,
        bool brownout)
    {
        Kind = kind;
        TotalSupply = totalSupply;
        TotalDemand = totalDemand;
        ReachableDemand = reachableDemand;
        ServedDemand = servedDemand;
        ServedConsumers = servedConsumers;
        ReachableConsumers = reachableConsumers;
        Brownout = brownout;
    }

    /// <summary>Fraction of reachable demand that was served (1 = fully supplied).</summary>
    public float ServedFraction => ReachableDemand > 0f ? ServedDemand / ReachableDemand : 1f;
}
