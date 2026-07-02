using CityBuilder.Common;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Simulation;

namespace CityBuilder.Zoning;

/// <summary>
/// Simulation system that grows the city. On its (slow) tick it performs heat-map
/// maintenance (diffuse desirability, decay pollution/crime), advances the cellular
/// automaton one generation, then publishes an aggregate <see cref="ZoningUpdatedEvent"/>
/// for the UI. It reads/writes only world data and raises events — no rendering, no engine.
/// </summary>
public sealed class ZoningSystem : ISimulationSystem
{
    private readonly WorldMap _map;
    private readonly HeatMapRegistry _heat;
    private readonly CellularAutomataEngine _automata;
    private readonly IEventBus _events;
    private readonly DeterministicRandom _rng;

    private int _previousDeveloped;

    public ZoningSystem(
        WorldMap map,
        HeatMapRegistry heat,
        CellularAutomataEngine automata,
        IEventBus events,
        DeterministicRandom rng)
    {
        _map = map;
        _heat = heat;
        _automata = automata;
        _events = events;
        _rng = rng;
    }

    public string Name => "Zoning";

    public int TickInterval => Simulation.TickInterval.Slow;

    public void OnTick(in TickContext context)
    {
        MaintainHeatMaps();

        _automata.Step(_map.Zoning, _heat, _rng);

        int developed = CountDeveloped();
        int changed = Math.Abs(developed - _previousDeveloped);
        _previousDeveloped = developed;

        _events.Publish(new ZoningUpdatedEvent(context.Tick, developed, changed));
    }

    /// <summary>Let the scalar fields spread and fade so growth pressure evolves over time.</summary>
    private void MaintainHeatMaps()
    {
        if (_heat.TryGet(HeatMapKind.Desirability, out HeatMap desirability))
        {
            desirability.Diffuse(0.20f);
        }

        if (_heat.TryGet(HeatMapKind.Pollution, out HeatMap pollution))
        {
            pollution.Diffuse(0.15f);
            pollution.Decay(0.98f);
        }

        if (_heat.TryGet(HeatMapKind.Crime, out HeatMap crime))
        {
            crime.Decay(0.99f);
        }
    }

    private int CountDeveloped()
    {
        Span<ZoneCell> cells = _map.Zoning.AsSpan();
        int count = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].IsDeveloped)
            {
                count++;
            }
        }

        return count;
    }
}
