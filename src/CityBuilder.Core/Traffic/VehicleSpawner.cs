using CityBuilder.Common;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;

namespace CityBuilder.Traffic;

/// <summary>
/// Creates vehicle agents on the road network and assigns each a route. Routing uses A* with
/// the LIVE congestion weights, so a vehicle spawned into a jam is routed around it. The
/// vehicle entity is produced by the data-driven <see cref="EntityFactory"/>; its route buffer
/// comes from the pooled <see cref="RouteTable"/>.
/// </summary>
public sealed class VehicleSpawner
{
    private readonly EcsWorld _world;
    private readonly EntityFactory _factory;
    private readonly FlowNetwork _road;
    private readonly IPathfinder _pathfinder;
    private readonly CongestionWeightProvider _congestion;
    private readonly RouteTable _routes;
    private readonly IEventBus _events;
    private readonly string _vehicleDefinitionId;

    public VehicleSpawner(
        EcsWorld world,
        EntityFactory factory,
        FlowNetwork road,
        IPathfinder pathfinder,
        CongestionWeightProvider congestion,
        RouteTable routes,
        IEventBus events,
        string vehicleDefinitionId)
    {
        _world = world;
        _factory = factory;
        _road = road;
        _pathfinder = pathfinder;
        _congestion = congestion;
        _routes = routes;
        _events = events;
        _vehicleDefinitionId = vehicleDefinitionId;
    }

    /// <summary>Spawn a vehicle routed from one road node to another. Returns Entity.None if unroutable.</summary>
    public Entity Spawn(int startNode, int goalNode, long tick)
    {
        int n = _road.NodeCount;
        if (startNode == goalNode || startNode < 0 || goalNode < 0 || startNode >= n || goalNode >= n)
        {
            return Entity.None;
        }

        List<int> buffer = _routes.RentBuffer();
        PathResult result = _pathfinder.FindPath(_road, startNode, goalNode, buffer, _congestion);
        if (!result.Found || buffer.Count < 2)
        {
            _routes.ReturnBuffer(buffer);
            return Entity.None;
        }

        GridCoord startCoord = _road.GetCoord(startNode);
        Entity vehicle = _factory.CreateVehicle(_vehicleDefinitionId, startCoord);
        _routes.Assign(vehicle, buffer, tick);

        // Place the agent on the first edge of its route.
        ref MovementComponent m = ref _world.Get<MovementComponent>(vehicle);
        m.PathCursor = 0;
        m.FromNode = buffer[0];
        m.ToNode = buffer[1];
        m.Progress = 0f;
        m.Active = true;
        if (_road.TryGetNeighbor(m.FromNode, m.ToNode, out PathNeighbor first))
        {
            m.Edge = first.Edge;
            m.EdgeLength = MathF.Max(0.001f, first.BaseCost);
            _congestion.Enter(m.Edge);
        }

        _events.Publish(new VehicleSpawnedEvent(vehicle, startNode, goalNode, buffer.Count));
        return vehicle;
    }

    /// <summary>Spawn a vehicle between two random road nodes (deterministic given the RNG).</summary>
    public Entity SpawnRandom(DeterministicRandom rng, long tick)
    {
        int n = _road.NodeCount;
        if (n < 2)
        {
            return Entity.None;
        }

        int start = rng.NextInt(0, n);
        int goal = rng.NextInt(0, n);
        return Spawn(start, goal, tick);
    }
}
