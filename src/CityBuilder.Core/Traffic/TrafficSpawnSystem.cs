using CityBuilder.Common;
using CityBuilder.Simulation;

namespace CityBuilder.Traffic;

/// <summary>
/// Keeps a steady stream of traffic on the network: every N ticks it tops up the active
/// vehicle count toward a cap using the <see cref="VehicleSpawner"/>. Registering this at a
/// coarser <see cref="TickInterval"/> than the movement system demonstrates the scheduler's
/// variable cadences. Deterministic (drives the shared seeded RNG).
/// </summary>
public sealed class TrafficSpawnSystem : ISimulationSystem
{
    private readonly VehicleSpawner _spawner;
    private readonly RouteTable _routes;
    private readonly DeterministicRandom _rng;
    private readonly int _maxActive;
    private readonly int _spawnPerTick;

    public TrafficSpawnSystem(
        VehicleSpawner spawner,
        RouteTable routes,
        DeterministicRandom rng,
        int tickInterval,
        int maxActive,
        int spawnPerTick = 1)
    {
        _spawner = spawner;
        _routes = routes;
        _rng = rng;
        TickInterval = Math.Max(1, tickInterval);
        _maxActive = maxActive;
        _spawnPerTick = Math.Max(1, spawnPerTick);
    }

    public string Name => "TrafficSpawn";

    public int TickInterval { get; }

    public void OnTick(in TickContext context)
    {
        for (int i = 0; i < _spawnPerTick && _routes.ActiveCount < _maxActive; i++)
        {
            _spawner.SpawnRandom(_rng, context.Tick);
        }
    }
}
