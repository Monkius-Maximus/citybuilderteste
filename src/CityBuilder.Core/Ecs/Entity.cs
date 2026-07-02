namespace CityBuilder.Ecs;

/// <summary>
/// A lightweight entity handle: just an id plus a generation counter. Entities own no
/// data and no behaviour — components (plain structs) hold the data, systems hold the
/// logic. This is composition over inheritance: no deep class trees, ever.
/// <para>
/// The <see cref="Generation"/> makes stale handles detectable: when an id is recycled
/// its generation is bumped, so a handle captured before destruction no longer matches.
/// </para>
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>1-based id; 0 means "no entity".</summary>
    public readonly int Id;

    public readonly int Generation;

    public Entity(int id, int generation)
    {
        Id = id;
        Generation = generation;
    }

    public static readonly Entity None = default;

    public bool IsValid => Id != 0;

    public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => unchecked((Id * 397) ^ Generation);

    public static bool operator ==(Entity a, Entity b) => a.Equals(b);

    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);

    public override string ToString() => IsValid ? $"Entity#{Id}.{Generation}" : "Entity.None";
}
