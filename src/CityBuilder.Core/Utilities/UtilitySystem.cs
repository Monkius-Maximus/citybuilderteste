using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Simulation;

namespace CityBuilder.Utilities;

/// <summary>
/// Solves every registered <see cref="UtilityGrid"/> on a slow cadence and publishes a report
/// per service. Grids are added after construction (once their networks/sources exist), so the
/// system holds a mutable list. Pure data + events — no rendering.
/// </summary>
public sealed class UtilitySystem : ISimulationSystem
{
    private readonly List<UtilityGrid> _grids = new();
    private readonly IEventBus _events;

    public UtilitySystem(IEventBus events) => _events = events;

    public IReadOnlyList<UtilityGrid> Grids => _grids;

    public void AddGrid(UtilityGrid grid) => _grids.Add(grid);

    public string Name => "Utilities";

    public int TickInterval => Simulation.TickInterval.Slow;

    public void OnTick(in TickContext context)
    {
        for (int i = 0; i < _grids.Count; i++)
        {
            UtilityReport r = _grids[i].Solve();
            _events.Publish(new UtilityUpdatedEvent(
                r.Kind, r.TotalSupply, r.TotalDemand, r.ReachableDemand,
                r.ServedDemand, r.ServedConsumers, r.ReachableConsumers, r.Brownout));
        }
    }
}
