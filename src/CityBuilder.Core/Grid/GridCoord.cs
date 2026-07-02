namespace CityBuilder.Grid;

/// <summary>
/// Integer cell coordinate in logical (grid) space. This is the simulation's
/// unit of location — the presentation layer converts it to screen pixels via
/// <see cref="IsometricProjector"/>. Kept a small <c>readonly struct</c> so it
/// lives on the stack / packs tightly into component arrays (cache friendliness).
/// </summary>
public readonly struct GridCoord : IEquatable<GridCoord>
{
    public readonly int X;
    public readonly int Y;

    public GridCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static readonly GridCoord Zero = new(0, 0);

    // The four orthogonal neighbours (road/pipe connectivity uses these).
    public static readonly GridCoord North = new(0, -1);
    public static readonly GridCoord South = new(0, 1);
    public static readonly GridCoord East = new(1, 0);
    public static readonly GridCoord West = new(-1, 0);

    public GridCoord Offset(int dx, int dy) => new(X + dx, Y + dy);

    public static GridCoord operator +(GridCoord a, GridCoord b) => new(a.X + b.X, a.Y + b.Y);

    public static GridCoord operator -(GridCoord a, GridCoord b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>Manhattan distance — the natural metric on a 4-connected grid.</summary>
    public static int ManhattanDistance(GridCoord a, GridCoord b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>Chebyshev distance — used when diagonal movement costs the same as orthogonal.</summary>
    public static int ChebyshevDistance(GridCoord a, GridCoord b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public bool Equals(GridCoord other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is GridCoord other && Equals(other);

    public override int GetHashCode() => unchecked((X * 397) ^ Y);

    public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);

    public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);

    public override string ToString() => $"({X}, {Y})";
}
