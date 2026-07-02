namespace CityBuilder.Simulation;

/// <summary>
/// Suggested cadences expressed as multiples of the base tick, so heavy subsystems
/// don't run every tick. With a 10 Hz base rate: Fast = 10×/s, Normal = 1×/s,
/// Slow ≈ every 6 s, Glacial ≈ every 60 s. These are just conventions — a system may
/// return any positive interval from <c>ISimulationSystem.TickInterval</c>.
/// </summary>
public static class TickInterval
{
    /// <summary>Every base tick. Movement / vehicle stepping.</summary>
    public const int Fast = 1;

    /// <summary>Roughly once per simulated second (at a 10 Hz base). Traffic, power flow.</summary>
    public const int Normal = 10;

    /// <summary>Every few seconds. Zoning growth, cellular-automata passes.</summary>
    public const int Slow = 60;

    /// <summary>Coarse, expensive passes. Economy settlement, demographics.</summary>
    public const int Glacial = 600;
}
