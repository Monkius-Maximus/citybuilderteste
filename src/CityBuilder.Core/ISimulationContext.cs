using CityBuilder.Ecs;
using CityBuilder.Events;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Zoning;

namespace CityBuilder;

/// <summary>
/// The read/write surface of the running simulation that commands and systems act upon.
/// It aggregates the world state (map, entities, networks, heat-maps) and the event bus.
/// Commands receive this instead of concrete singletons, which keeps them testable and is
/// what will let a lockstep multiplayer host apply the same command stream on every peer.
/// </summary>
public interface ISimulationContext
{
    WorldMap Map { get; }
    EcsWorld Entities { get; }
    IEventBus Events { get; }
    HeatMapRegistry HeatMaps { get; }

    /// <summary>Current simulation tick — commands may stamp their effects with it.</summary>
    long CurrentTick { get; }

    /// <summary>Fetch (creating if needed) the graph for a transport/utility layer.</summary>
    FlowNetwork GetNetwork(NetworkType type);
}
