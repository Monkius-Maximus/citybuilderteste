namespace CityBuilder.Pathfinding;

/// <summary>
/// Outcome of a path query. The node sequence itself is written into a caller-supplied
/// buffer (so it can be pooled/reused); this struct just reports success, total cost and
/// length — no allocation.
/// </summary>
public readonly struct PathResult
{
    public readonly bool Found;
    public readonly float Cost;
    public readonly int NodeCount;

    public PathResult(bool found, float cost, int nodeCount)
    {
        Found = found;
        Cost = cost;
        NodeCount = nodeCount;
    }

    public static readonly PathResult NotFound = new(false, 0f, 0);
}
