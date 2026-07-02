namespace CityBuilder.Simulation;

/// <summary>
/// Drives all registered <see cref="ISimulationSystem"/>s off a single
/// <see cref="SimulationClock"/>. Each frame the host calls <see cref="Update"/> with a
/// real delta; the scheduler runs whole fixed ticks and, on each tick, invokes only the
/// systems whose interval divides the current tick. Deterministic and framerate-independent.
/// </summary>
public sealed class SimulationScheduler
{
    private readonly List<ISimulationSystem> _systems = new();
    private readonly SimulationClock _clock;

    public SimulationScheduler(SimulationClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public SimulationClock Clock => _clock;

    public long CurrentTick => _clock.CurrentTick;

    public IReadOnlyList<ISimulationSystem> Systems => _systems;

    /// <summary>Register a system. Order of registration is the execution order within a tick.</summary>
    public void Register(ISimulationSystem system)
    {
        if (system is null)
        {
            throw new ArgumentNullException(nameof(system));
        }

        if (system.TickInterval < 1)
        {
            throw new ArgumentException($"System '{system.Name}' has an invalid TickInterval < 1.", nameof(system));
        }

        _systems.Add(system);
    }

    /// <summary>Call <see cref="IInitializableSystem.Initialize"/> on systems that need it.</summary>
    public void InitializeSystems()
    {
        foreach (ISimulationSystem system in _systems)
        {
            if (system is IInitializableSystem init)
            {
                init.Initialize();
            }
        }
    }

    /// <summary>
    /// Host entry point: feed variable real time (seconds). Runs 0..N fixed ticks.
    /// Returns the number of ticks actually processed.
    /// </summary>
    public int Update(double realDeltaSeconds)
    {
        int ticks = _clock.Advance(realDeltaSeconds);
        for (int i = 0; i < ticks; i++)
        {
            StepOneTick();
        }

        return ticks;
    }

    /// <summary>
    /// Advance exactly one fixed tick regardless of the clock's accumulator or speed
    /// (used by headless runners, deterministic tests and lockstep networking that steps
    /// on command boundaries).
    /// </summary>
    public void StepOnce() => StepOneTick();

    private void StepOneTick()
    {
        long tick = _clock.ConsumeTick();
        double seconds = _clock.GameSeconds;

        // Run each system only on ticks that are a multiple of its interval.
        for (int i = 0; i < _systems.Count; i++)
        {
            ISimulationSystem system = _systems[i];
            if (tick % system.TickInterval == 0)
            {
                double delta = system.TickInterval * _clock.SecondsPerTick;
                var context = new TickContext(tick, system.TickInterval, seconds, delta);
                system.OnTick(in context);
            }
        }
    }
}
