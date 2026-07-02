namespace CityBuilder.Ecs;

/// <summary>
/// Sparse-set storage for one component type. Components live in a densely packed
/// array (<see cref="Components"/>) so a system can iterate them linearly with no gaps
/// and no cache misses, while <c>_sparse</c> gives O(1) lookup/insert/remove by entity id.
/// Removal is swap-back, keeping the dense array compact.
/// </summary>
public sealed class ComponentStore<T> : IComponentStore where T : struct, IComponent
{
    // _sparse[entityId] = denseIndex + 1  (0 means "absent"). Grows with entity ids.
    private int[] _sparse;

    // Packed, parallel arrays. _dense[i] belongs to entity _owners[i].
    private T[] _dense;
    private Entity[] _owners;
    private int _count;

    public ComponentStore(int initialCapacity = 64)
    {
        _sparse = new int[initialCapacity];
        _dense = new T[initialCapacity];
        _owners = new Entity[initialCapacity];
    }

    public Type ComponentType => typeof(T);

    public int Count => _count;

    /// <summary>The packed components — iterate this directly in tick systems.</summary>
    public Span<T> Components => _dense.AsSpan(0, _count);

    /// <summary>The entity that owns each packed component (parallel to <see cref="Components"/>).</summary>
    public ReadOnlySpan<Entity> Owners => _owners.AsSpan(0, _count);

    public bool Has(Entity entity)
        => entity.Id > 0 && entity.Id < _sparse.Length && _sparse[entity.Id] != 0;

    /// <summary>Add or overwrite the component for an entity.</summary>
    public ref T Add(Entity entity, in T value)
    {
        EnsureSparse(entity.Id);

        int slot = _sparse[entity.Id];
        if (slot != 0)
        {
            int existing = slot - 1;
            _dense[existing] = value;
            return ref _dense[existing];
        }

        if (_count == _dense.Length)
        {
            GrowDense();
        }

        _dense[_count] = value;
        _owners[_count] = entity;
        _sparse[entity.Id] = _count + 1;
        return ref _dense[_count++];
    }

    /// <summary>Get a mutable reference to an entity's component. Throws if absent.</summary>
    public ref T Get(Entity entity)
    {
        int slot = entity.Id < _sparse.Length ? _sparse[entity.Id] : 0;
        if (slot == 0)
        {
            throw new KeyNotFoundException($"Entity {entity} has no {typeof(T).Name} component.");
        }

        return ref _dense[slot - 1];
    }

    public bool TryGetIndex(Entity entity, out int denseIndex)
    {
        int slot = entity.Id > 0 && entity.Id < _sparse.Length ? _sparse[entity.Id] : 0;
        denseIndex = slot - 1;
        return slot != 0;
    }

    public bool Remove(Entity entity)
    {
        if (entity.Id <= 0 || entity.Id >= _sparse.Length)
        {
            return false;
        }

        int slot = _sparse[entity.Id];
        if (slot == 0)
        {
            return false;
        }

        int removeAt = slot - 1;
        int lastIndex = _count - 1;

        // Swap the last element into the removed slot to keep the array packed.
        if (removeAt != lastIndex)
        {
            _dense[removeAt] = _dense[lastIndex];
            Entity moved = _owners[lastIndex];
            _owners[removeAt] = moved;
            _sparse[moved.Id] = removeAt + 1;
        }

        _dense[lastIndex] = default;
        _owners[lastIndex] = Entity.None;
        _sparse[entity.Id] = 0;
        _count--;
        return true;
    }

    private void EnsureSparse(int entityId)
    {
        if (entityId < _sparse.Length)
        {
            return;
        }

        int newSize = _sparse.Length;
        while (newSize <= entityId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _sparse, newSize);
    }

    private void GrowDense()
    {
        int newSize = _dense.Length * 2;
        Array.Resize(ref _dense, newSize);
        Array.Resize(ref _owners, newSize);
    }
}
