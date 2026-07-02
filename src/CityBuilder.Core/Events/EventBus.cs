namespace CityBuilder.Events;

/// <summary>
/// Copy-on-write pub/sub implementation. Each event type gets a channel holding an
/// immutable handler array; publishing iterates the current snapshot, so handlers may
/// safely subscribe/unsubscribe during dispatch and publishing allocates nothing.
/// Single-threaded, matching the simulation's single logical tick thread.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, object> _channels = new();

    public IDisposable Subscribe<T>(Action<T> handler) where T : IEvent
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        GetOrCreateChannel<T>().Add(handler);
        return new Subscription<T>(this, handler);
    }

    public void Publish<T>(in T evt) where T : IEvent
    {
        if (_channels.TryGetValue(typeof(T), out object? channel))
        {
            ((Channel<T>)channel).Publish(evt);
        }
    }

    private Channel<T> GetOrCreateChannel<T>() where T : IEvent
    {
        if (!_channels.TryGetValue(typeof(T), out object? channel))
        {
            channel = new Channel<T>();
            _channels[typeof(T)] = channel;
        }

        return (Channel<T>)channel;
    }

    private void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        if (_channels.TryGetValue(typeof(T), out object? channel))
        {
            ((Channel<T>)channel).Remove(handler);
        }
    }

    /// <summary>Immutable handler list for one event type (copy-on-write).</summary>
    private sealed class Channel<T> where T : IEvent
    {
        private Action<T>[] _handlers = Array.Empty<Action<T>>();

        public void Add(Action<T> handler)
        {
            var updated = new Action<T>[_handlers.Length + 1];
            Array.Copy(_handlers, updated, _handlers.Length);
            updated[_handlers.Length] = handler;
            _handlers = updated;
        }

        public void Remove(Action<T> handler)
        {
            int index = Array.IndexOf(_handlers, handler);
            if (index < 0)
            {
                return;
            }

            if (_handlers.Length == 1)
            {
                _handlers = Array.Empty<Action<T>>();
                return;
            }

            var updated = new Action<T>[_handlers.Length - 1];
            Array.Copy(_handlers, 0, updated, 0, index);
            Array.Copy(_handlers, index + 1, updated, index, _handlers.Length - index - 1);
            _handlers = updated;
        }

        public void Publish(in T evt)
        {
            Action<T>[] snapshot = _handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i](evt);
            }
        }
    }

    /// <summary>Disposable subscription handle returned to callers for unsubscription.</summary>
    private sealed class Subscription<T> : IDisposable where T : IEvent
    {
        private EventBus? _bus;
        private readonly Action<T> _handler;

        public Subscription(EventBus bus, Action<T> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            _bus?.Unsubscribe(_handler);
            _bus = null;
        }
    }
}
