namespace CityBuilder.Events;

/// <summary>
/// Marker for a simulation event/notification. Events are immutable value snapshots the
/// simulation publishes; the UI and other observers react to them. The simulation never
/// calls the UI directly — it only raises events, keeping presentation fully decoupled.
/// </summary>
public interface IEvent
{
}
