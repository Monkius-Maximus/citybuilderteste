namespace CityBuilder.Networks;

/// <summary>Dense 0-based handle to a node inside one <see cref="FlowNetwork"/> (usable as an array index).</summary>
public readonly struct NodeId : IEquatable<NodeId>
{
    public readonly int Value;

    public NodeId(int value) => Value = value;

    public static readonly NodeId Invalid = new(-1);

    public bool IsValid => Value >= 0;

    public bool Equals(NodeId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is NodeId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => $"N{Value}";
}

/// <summary>Dense 0-based handle to an edge inside one <see cref="FlowNetwork"/>.</summary>
public readonly struct EdgeId : IEquatable<EdgeId>
{
    public readonly int Value;

    public EdgeId(int value) => Value = value;

    public static readonly EdgeId Invalid = new(-1);

    public bool IsValid => Value >= 0;

    public bool Equals(EdgeId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is EdgeId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => $"E{Value}";
}
