using CityBuilder.Grid;

namespace CityBuilder.Pathfinding;

/// <summary>
/// Admissible distance estimate used by A* to prioritise its search. Must never
/// overestimate the true remaining cost, or the returned path may be non-optimal.
/// </summary>
public interface IHeuristic
{
    float Estimate(GridCoord from, GridCoord to);
}

/// <summary>Ready-made heuristics for grid graphs.</summary>
public static class Heuristics
{
    /// <summary>h = 0. Turns A* into Dijkstra (explores uniformly, ignores the goal).</summary>
    public static readonly IHeuristic Zero = new ZeroHeuristic();

    /// <summary>4-connected grids (orthogonal movement only).</summary>
    public static readonly IHeuristic Manhattan = new ManhattanHeuristic();

    /// <summary>8-connected grids (diagonal moves cost the same as orthogonal).</summary>
    public static readonly IHeuristic Chebyshev = new ChebyshevHeuristic();

    /// <summary>Straight-line distance (good when movement is near-continuous).</summary>
    public static readonly IHeuristic Euclidean = new EuclideanHeuristic();

    private sealed class ZeroHeuristic : IHeuristic
    {
        public float Estimate(GridCoord from, GridCoord to) => 0f;
    }

    private sealed class ManhattanHeuristic : IHeuristic
    {
        public float Estimate(GridCoord from, GridCoord to) => GridCoord.ManhattanDistance(from, to);
    }

    private sealed class ChebyshevHeuristic : IHeuristic
    {
        public float Estimate(GridCoord from, GridCoord to) => GridCoord.ChebyshevDistance(from, to);
    }

    private sealed class EuclideanHeuristic : IHeuristic
    {
        public float Estimate(GridCoord from, GridCoord to)
        {
            float dx = from.X - to.X;
            float dy = from.Y - to.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
