namespace CityBuilder.Common;

/// <summary>
/// Generic, allocation-free object pool for high-churn simulation actors
/// (vehicles, pedestrians, particles). Reusing instances keeps the GC quiet
/// during real-time play, which is mandatory per the architecture brief.
/// <para>Not thread-safe: the simulation tick runs on a single logical thread.</para>
/// </summary>
public sealed class ObjectPool<T> where T : class
{
    private readonly Stack<T> _free;
    private readonly Func<T> _factory;
    private readonly Action<T>? _onRent;
    private readonly Action<T>? _onReturn;
    private readonly int _maxRetained;

    private int _liveCount;

    /// <param name="factory">Creates a fresh instance when the pool is empty.</param>
    /// <param name="onRent">Optional hook invoked when an instance is handed out.</param>
    /// <param name="onReturn">Optional hook invoked when an instance is recycled.</param>
    /// <param name="prewarm">Number of instances to pre-allocate up front.</param>
    /// <param name="maxRetained">Upper bound on pooled (idle) instances; extras are dropped for the GC.</param>
    public ObjectPool(
        Func<T> factory,
        Action<T>? onRent = null,
        Action<T>? onReturn = null,
        int prewarm = 0,
        int maxRetained = int.MaxValue)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _onRent = onRent;
        _onReturn = onReturn;
        _maxRetained = maxRetained;
        _free = new Stack<T>(prewarm > 0 ? prewarm : 16);

        for (int i = 0; i < prewarm; i++)
        {
            _free.Push(_factory());
        }
    }

    /// <summary>Instances currently rented out (not sitting in the pool).</summary>
    public int LiveCount => _liveCount;

    /// <summary>Instances currently idle in the pool.</summary>
    public int FreeCount => _free.Count;

    /// <summary>Take an instance from the pool (or create one).</summary>
    public T Rent()
    {
        T item = _free.Count > 0 ? _free.Pop() : _factory();
        _liveCount++;

        _onRent?.Invoke(item);
        if (item is IPoolable poolable)
        {
            poolable.OnRent();
        }

        return item;
    }

    /// <summary>Return an instance to the pool for reuse.</summary>
    public void Return(T item)
    {
        if (item is null)
        {
            return;
        }

        _onReturn?.Invoke(item);
        if (item is IPoolable poolable)
        {
            poolable.OnReturn();
        }

        if (_liveCount > 0)
        {
            _liveCount--;
        }

        // Drop the instance if we already retain enough idle copies.
        if (_free.Count < _maxRetained)
        {
            _free.Push(item);
        }
    }
}
