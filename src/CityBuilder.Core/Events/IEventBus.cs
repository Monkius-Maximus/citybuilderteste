namespace CityBuilder.Events;

/// <summary>
/// Type-safe publish/subscribe hub. Producers (simulation systems) publish events;
/// consumers (UI, audio, analytics) subscribe by event type. Neither side references
/// the other — this is the seam that lets the same simulation run headless or under any engine.
/// </summary>
public interface IEventBus
{
    /// <summary>Subscribe to events of type <typeparamref name="T"/>. Dispose the token to unsubscribe.</summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : IEvent;

    /// <summary>Publish an event to all current subscribers of its type.</summary>
    void Publish<T>(in T evt) where T : IEvent;
}
