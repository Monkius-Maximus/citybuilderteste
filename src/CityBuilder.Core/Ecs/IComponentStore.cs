namespace CityBuilder.Ecs;

/// <summary>
/// Non-generic facet of a component store so the <see cref="EcsWorld"/> can hold a
/// heterogeneous collection of stores and clean them all up when an entity dies.
/// </summary>
public interface IComponentStore
{
    Type ComponentType { get; }

    int Count { get; }

    /// <summary>Remove this entity's component if present. Returns true if something was removed.</summary>
    bool Remove(Entity entity);
}
