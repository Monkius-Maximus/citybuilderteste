namespace CityBuilder.Simulation;

/// <summary>
/// Immutable snapshot handed to a system on each of its ticks. Everything here is
/// integer/double time — never wall-clock or frame-time — so behaviour is identical
/// regardless of rendering framerate or machine speed (determinism requirement).
/// </summary>
public readonly struct TickContext
{
    /// <summary>Global base-tick counter since the simulation started.</summary>
    public readonly long Tick;

    /// <summary>How many base ticks elapsed for THIS system since its previous run (its interval).</summary>
    public readonly int Interval;

    /// <summary>Total simulated time in seconds (derived from <see cref="Tick"/>, deterministic).</summary>
    public readonly double GameSeconds;

    public TickContext(long tick, int interval, double gameSeconds)
    {
        Tick = tick;
        Interval = interval;
        GameSeconds = gameSeconds;
    }
}
