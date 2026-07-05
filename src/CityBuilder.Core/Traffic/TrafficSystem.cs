using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Simulation;

namespace CityBuilder.Traffic;

/// <summary>
/// Advances every routed vehicle along its path each fast tick. It integrates progress with the
/// deterministic <see cref="TickContext.DeltaSeconds"/>, hands vehicles from edge to edge,
/// maintains live edge congestion (which feeds routing), and despawns vehicles on arrival —
/// recycling their entity id and pooled route buffer. Pure data mutation + events; no rendering.
/// </summary>
public sealed class TrafficSystem : ISimulationSystem
{
    private readonly EcsWorld _world;
    private readonly FlowNetwork _road;
    private readonly CongestionWeightProvider _congestion;
    private readonly RouteTable _routes;
    private readonly IEventBus _events;

    // Vehicles that left the traffic flow this tick, resolved after the movement pass so that
    // store removals / arrival-event handlers never disturb the span being iterated.
    private readonly List<Departure> _departures = new();

    public TrafficSystem(
        EcsWorld world,
        FlowNetwork road,
        CongestionWeightProvider congestion,
        RouteTable routes,
        IEventBus events)
    {
        _world = world;
        _road = road;
        _congestion = congestion;
        _routes = routes;
        _events = events;
    }

    public string Name => "Traffic";

    public int TickInterval => Simulation.TickInterval.Fast;

    public void OnTick(in TickContext context)
    {
        ComponentStore<MovementComponent> store = _world.Store<MovementComponent>();
        Span<MovementComponent> comps = store.Components;
        ReadOnlySpan<Entity> owners = store.Owners;

        float dt = (float)context.DeltaSeconds;
        double secondsPerTick = context.Interval > 0 ? context.DeltaSeconds / context.Interval : context.DeltaSeconds;

        _departures.Clear();

        for (int i = 0; i < comps.Length; i++)
        {
            ref MovementComponent m = ref comps[i];
            if (!m.Active)
            {
                continue;
            }

            Entity entity = owners[i];
            m.Progress += m.Speed * dt / m.EdgeLength;

            // A single tick may cross more than one (short) edge.
            while (m.Progress >= 1f && m.Active)
            {
                m.Progress -= 1f;
                CrossNode(entity, ref m, context.GameSeconds, secondsPerTick);
            }

            // Snap the visual position to the node currently being left (renderer interpolates the rest).
            if (m.Active && _world.Has<GridPositionComponent>(entity))
            {
                ref GridPositionComponent pos = ref _world.Get<GridPositionComponent>(entity);
                pos.Cell = _road.GetCoord(m.FromNode);
            }
        }

        // Resolve departures after the pass: emit arrival events, recycle routes and entity ids.
        for (int i = 0; i < _departures.Count; i++)
        {
            Departure d = _departures[i];
            if (d.Arrived)
            {
                _events.Publish(new VehicleArrivedEvent(d.Entity, d.DestinationNode, d.TravelSeconds));
            }

            _routes.Release(d.Entity);
            _world.Destroy(d.Entity);
        }
    }

    /// <summary>The agent reached <see cref="MovementComponent.ToNode"/>; move it to the next edge or finish.</summary>
    private void CrossNode(Entity entity, ref MovementComponent m, double gameSeconds, double secondsPerTick)
    {
        _congestion.Leave(m.Edge);

        if (!_routes.TryGet(entity, out RouteEntry route))
        {
            m.Active = false;
            _departures.Add(new Departure(entity, m.ToNode, 0, arrived: false));
            return;
        }

        int nextCursor = m.PathCursor + 1;

        // If ToNode was the last node, the trip is complete.
        if (nextCursor + 1 >= route.Count)
        {
            m.Active = false;
            double travel = gameSeconds - route.SpawnTick * secondsPerTick;
            _departures.Add(new Departure(entity, m.ToNode, travel, arrived: true));
            return;
        }

        // Step onto the next edge.
        m.PathCursor = nextCursor;
        m.FromNode = route[nextCursor];
        m.ToNode = route[nextCursor + 1];

        if (_road.TryGetNeighbor(m.FromNode, m.ToNode, out PathNeighbor next))
        {
            m.Edge = next.Edge;
            m.EdgeLength = MathF.Max(0.001f, next.BaseCost);
            _congestion.Enter(m.Edge);
        }
        else
        {
            // The route was invalidated (edge removed); stop gracefully.
            m.Active = false;
            _departures.Add(new Departure(entity, m.ToNode, 0, arrived: false));
        }
    }

    private readonly struct Departure
    {
        public readonly Entity Entity;
        public readonly int DestinationNode;
        public readonly double TravelSeconds;
        public readonly bool Arrived;

        public Departure(Entity entity, int destinationNode, double travelSeconds, bool arrived)
        {
            Entity = entity;
            DestinationNode = destinationNode;
            TravelSeconds = travelSeconds;
            Arrived = arrived;
        }
    }
}
