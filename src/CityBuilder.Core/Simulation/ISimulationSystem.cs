namespace CityBuilder.Simulation;

/// <summary>
/// A pluggable simulation subsystem (economy, traffic, population growth, zoning...).
/// Systems are pure logic that mutate world state; they subscribe to the scheduler at
/// a chosen cadence (<see cref="TickInterval"/>) and receive a deterministic
/// <see cref="TickContext"/>. They know nothing about rendering.
/// </summary>
public interface ISimulationSystem
{
    /// <summary>Human-readable id for logging / profiling.</summary>
    string Name { get; }

    /// <summary>Run this system once every N base ticks. Must be ≥ 1.</summary>
    int TickInterval { get; }

    /// <summary>Advance this system by one of its ticks.</summary>
    void OnTick(in TickContext context);
}

/// <summary>
/// Optional companion contract for systems that need one-time setup once all systems
/// and world state are registered (wiring event subscriptions, caching layer handles).
/// </summary>
public interface IInitializableSystem
{
    void Initialize();
}
