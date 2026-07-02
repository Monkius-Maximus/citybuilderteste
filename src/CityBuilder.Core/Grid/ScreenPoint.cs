namespace CityBuilder.Grid;

/// <summary>
/// A point in 2D screen/pixel space, produced by projecting a <see cref="GridCoord"/>.
/// This lives in Core (not the engine) so projection math can be unit-tested headless;
/// the presentation layer maps it onto whatever vector type the engine uses
/// (UnityEngine.Vector2, Godot.Vector2, ...).
/// </summary>
public readonly struct ScreenPoint : IEquatable<ScreenPoint>
{
    public readonly float X;
    public readonly float Y;

    public ScreenPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static ScreenPoint operator +(ScreenPoint a, ScreenPoint b) => new(a.X + b.X, a.Y + b.Y);

    public static ScreenPoint operator -(ScreenPoint a, ScreenPoint b) => new(a.X - b.X, a.Y - b.Y);

    public bool Equals(ScreenPoint other)
        => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is ScreenPoint other && Equals(other);

    public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());

    public override string ToString() => $"[{X:0.##}, {Y:0.##}]";
}
