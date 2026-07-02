namespace CityBuilder.Ecs;

/// <summary>
/// Allocates and recycles <see cref="Entity"/> ids with generational versioning.
/// Destroyed ids are reused (bounded memory) but their generation is bumped so any
/// handle captured before destruction is reliably detected as stale.
/// </summary>
public sealed class EntityRegistry
{
    private int[] _generations;
    private readonly Queue<int> _free = new();
    private int _next = 1; // ids are 1-based; 0 is Entity.None
    private int _aliveCount;

    public EntityRegistry(int initialCapacity = 64)
    {
        _generations = new int[Math.Max(1, initialCapacity)];
    }

    public int AliveCount => _aliveCount;

    public Entity Create()
    {
        int id;
        if (_free.Count > 0)
        {
            id = _free.Dequeue();
        }
        else
        {
            id = _next++;
            if (id >= _generations.Length)
            {
                Array.Resize(ref _generations, _generations.Length * 2);
            }
        }

        _aliveCount++;
        return new Entity(id, _generations[id]);
    }

    public bool IsAlive(Entity entity)
        => entity.Id > 0
           && entity.Id < _next
           && _generations[entity.Id] == entity.Generation;

    /// <summary>
    /// Retire an entity id. Bumps its generation and returns it to the free list.
    /// Component cleanup is the <see cref="EcsWorld"/>'s responsibility.
    /// </summary>
    public bool Destroy(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        _generations[entity.Id]++;
        _free.Enqueue(entity.Id);
        _aliveCount--;
        return true;
    }
}
