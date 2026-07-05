using CityBuilder.Common;
using CityBuilder.Ecs;

namespace CityBuilder.Traffic;

/// <summary>An assigned route: the ordered road-node ids plus when the agent departed.</summary>
public readonly struct RouteEntry
{
    public readonly List<int> Nodes;
    public readonly long SpawnTick;

    public RouteEntry(List<int> nodes, long spawnTick)
    {
        Nodes = nodes;
        SpawnTick = spawnTick;
    }

    public int Count => Nodes.Count;
    public int this[int index] => Nodes[index];
}

/// <summary>
/// Owns the per-vehicle routes. The node buffers are drawn from an <see cref="ObjectPool{T}"/>
/// and returned on despawn, so sustained traffic never churns the GC allocating path lists —
/// exactly the Object Pooling requirement, applied to routes.
/// </summary>
public sealed class RouteTable
{
    private readonly Dictionary<Entity, RouteEntry> _routes = new();
    private readonly ObjectPool<List<int>> _bufferPool;

    public RouteTable(int prewarm = 32)
    {
        _bufferPool = new ObjectPool<List<int>>(
            factory: () => new List<int>(16),
            onReturn: list => list.Clear(),
            prewarm: prewarm);
    }

    /// <summary>Number of vehicles that currently have an active route.</summary>
    public int ActiveCount => _routes.Count;

    /// <summary>Borrow a node buffer to fill with a freshly computed path.</summary>
    public List<int> RentBuffer() => _bufferPool.Rent();

    /// <summary>Return a buffer that was rented but not assigned (e.g. no path found).</summary>
    public void ReturnBuffer(List<int> buffer) => _bufferPool.Return(buffer);

    public void Assign(Entity entity, List<int> nodes, long spawnTick)
        => _routes[entity] = new RouteEntry(nodes, spawnTick);

    public bool TryGet(Entity entity, out RouteEntry entry) => _routes.TryGetValue(entity, out entry);

    /// <summary>Drop a vehicle's route and recycle its buffer.</summary>
    public bool Release(Entity entity)
    {
        if (_routes.TryGetValue(entity, out RouteEntry entry))
        {
            _routes.Remove(entity);
            _bufferPool.Return(entry.Nodes);
            return true;
        }

        return false;
    }
}
