namespace CityBuilder.Ecs;

/// <summary>
/// The ECS composition root: owns the <see cref="EntityRegistry"/> and one
/// <see cref="ComponentStore{T}"/> per registered component type. This is the "world"
/// that tick systems query and mutate. It has no engine dependency and no inheritance
/// hierarchy — behaviour lives entirely in systems, data lives entirely in components.
/// </summary>
public sealed class EcsWorld
{
    private readonly EntityRegistry _entities;
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    public EcsWorld(int initialCapacity = 256)
    {
        _entities = new EntityRegistry(initialCapacity);
    }

    public EntityRegistry Entities => _entities;

    public int AliveCount => _entities.AliveCount;

    /// <summary>Declare a component type before use (typically at bootstrap).</summary>
    public ComponentStore<T> RegisterComponent<T>(int initialCapacity = 64) where T : struct, IComponent
    {
        Type type = typeof(T);
        if (_stores.TryGetValue(type, out IComponentStore? existing))
        {
            return (ComponentStore<T>)existing;
        }

        var store = new ComponentStore<T>(initialCapacity);
        _stores[type] = store;
        return store;
    }

    /// <summary>Get the store for a component type (auto-registers if new).</summary>
    public ComponentStore<T> Store<T>() where T : struct, IComponent
    {
        if (_stores.TryGetValue(typeof(T), out IComponentStore? existing))
        {
            return (ComponentStore<T>)existing;
        }

        return RegisterComponent<T>();
    }

    public Entity Create() => _entities.Create();

    /// <summary>Destroy an entity and strip every component it owns from all stores.</summary>
    public bool Destroy(Entity entity)
    {
        if (!_entities.IsAlive(entity))
        {
            return false;
        }

        foreach (IComponentStore store in _stores.Values)
        {
            store.Remove(entity);
        }

        return _entities.Destroy(entity);
    }

    public bool IsAlive(Entity entity) => _entities.IsAlive(entity);

    // --- Component convenience API (delegates to the typed store) ---

    public ref T Add<T>(Entity entity, in T component) where T : struct, IComponent
        => ref Store<T>().Add(entity, in component);

    public bool Has<T>(Entity entity) where T : struct, IComponent
        => Store<T>().Has(entity);

    public ref T Get<T>(Entity entity) where T : struct, IComponent
        => ref Store<T>().Get(entity);

    public bool Remove<T>(Entity entity) where T : struct, IComponent
        => Store<T>().Remove(entity);
}
