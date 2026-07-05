namespace CityBuilder.Simulation;

/// <summary>
/// Fixed-timestep clock. Decouples the simulation rate from the render framerate:
/// the presentation layer feeds it a variable real delta each frame; the clock
/// accumulates and emits a whole number of fixed ticks to run. This is the standard
/// "fix your timestep" pattern and the foundation of deterministic simulation.
/// </summary>
public sealed class SimulationClock
{
    /// <summary>Base ticks per simulated second.</summary>
    public double TicksPerSecond { get; }

    public double SecondsPerTick { get; }

    /// <summary>Global tick counter (monotonic).</summary>
    public long CurrentTick { get; private set; }

    /// <summary>Simulated seconds elapsed (deterministic: CurrentTick × SecondsPerTick).</summary>
    public double GameSeconds => CurrentTick * SecondsPerTick;

    private double _accumulator;

    /// <summary>
    /// Speed multiplier applied to incoming real time (pause = 0, 1×, 2×, 3× ...).
    /// Changing it never affects determinism — it only scales how fast wall time is fed in.
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    /// <summary>Safety cap on ticks per Advance call, to avoid a "spiral of death" after a stall.</summary>
    public int MaxCatchUpTicks { get; set; } = 250;

    public SimulationClock(double ticksPerSecond = 10.0)
    {
        if (ticksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
        }

        TicksPerSecond = ticksPerSecond;
        SecondsPerTick = 1.0 / ticksPerSecond;
    }

    /// <summary>
    /// Feed variable real time; returns how many fixed ticks should be run now.
    /// The caller loops that many times, advancing systems once each via <see cref="ConsumeTick"/>.
    /// </summary>
    public int Advance(double realDeltaSeconds)
    {
        if (realDeltaSeconds <= 0 || SpeedMultiplier <= 0)
        {
            return 0;
        }

        _accumulator += realDeltaSeconds * SpeedMultiplier;

        int ticks = 0;
        while (_accumulator >= SecondsPerTick && ticks < MaxCatchUpTicks)
        {
            _accumulator -= SecondsPerTick;
            ticks++;
        }

        return ticks;
    }

    /// <summary>Advance the global counter by exactly one tick and return its index.</summary>
    public long ConsumeTick() => ++CurrentTick;

    /// <summary>Restore the tick counter from a save; discards any accumulated partial time.</summary>
    public void Restore(long tick)
    {
        CurrentTick = tick;
        _accumulator = 0;
    }

    /// <summary>Interpolation factor in [0,1) for smooth rendering between fixed ticks.</summary>
    public float Alpha => (float)(_accumulator / SecondsPerTick);
}
