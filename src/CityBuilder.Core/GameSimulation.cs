using CityBuilder.Commands;
using CityBuilder.Common;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Economy;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Population;
using CityBuilder.Simulation;
using CityBuilder.Traffic;
using CityBuilder.Utilities;
using CityBuilder.Zoning;
using CityBuilder.Zoning.Rules;

namespace CityBuilder;

/// <summary>
/// The composition root and single entry point for a running city. It owns all subsystems
/// (world map, ECS, networks, heat-maps, scheduler, command processor, factory) and wires
/// them together, then exposes a tiny driver surface (<see cref="Update"/>, <see cref="Step"/>,
/// <see cref="Submit"/>). A host — the console app here, or a Unity/Godot bootstrap later —
/// creates one of these and pumps it. Nothing in this class touches an engine type.
/// </summary>
public sealed class GameSimulation : ISimulationContext
{
    private readonly Dictionary<NetworkType, FlowNetwork> _networks = new();

    public GameConfig Config { get; }
    public WorldMap Map { get; }
    public EcsWorld Entities { get; }
    public HeatMapRegistry HeatMaps { get; }
    public DeterministicRandom Random { get; }

    public SimulationClock Clock { get; }
    public SimulationScheduler Scheduler { get; }
    public DefinitionRegistry Definitions { get; }
    public EntityFactory Factory { get; }
    public CommandProcessor Commands { get; }

    // Traffic layer: shared routing scratch, live road congestion, and the pooled route table.
    public RouteTable Routes { get; }
    public CongestionWeightProvider RoadCongestion { get; }
    public AStarPathfinder Pathfinder { get; }

    // Utilities layer: solves power/water coverage + capacity each slow tick.
    public UtilitySystem Utilities { get; }

    // Economy layer: taxes, markets, upkeep and the city treasury.
    public EconomySystem Economy { get; }

    // Population layer: RCI demand model (drives growth) + sector money circulation.
    public DemandModel Demand { get; }
    public PopulationSystem Population { get; }

    // Exposed as the interface type (exact match => clean implicit implementation of
    // ISimulationContext.Events). The concrete bus is held privately; callers only need
    // Subscribe/Publish, which IEventBus provides.
    private readonly EventBus _events;
    public IEventBus Events => _events;

    public long CurrentTick => Clock.CurrentTick;

    public GameSimulation(GameConfig config)
    {
        Config = config;

        Map = new WorldMap(config.Width, config.Height);
        Entities = new EcsWorld();
        _events = new EventBus();
        HeatMaps = new HeatMapRegistry(config.Width, config.Height);
        Random = new DeterministicRandom(config.Seed);

        Clock = new SimulationClock(config.TicksPerSecond);
        Scheduler = new SimulationScheduler(Clock);
        Definitions = new DefinitionRegistry();
        Factory = new EntityFactory(Entities, Definitions, Events);
        Commands = new CommandProcessor(this);

        Routes = new RouteTable();
        RoadCongestion = new CongestionWeightProvider();
        Pathfinder = new AStarPathfinder(Heuristics.Manhattan);
        Utilities = new UtilitySystem(Events);
        Economy = new EconomySystem(
            Map,
            GetNetwork(NetworkType.Road),
            Events,
            startingBalance: Money.FromWhole(50_000),
            settings: EconomySettings.Default,
            tickInterval: Simulation.TickInterval.Slow);

        DemandSettings demandSettings = DemandSettings.Default;
        Demand = new DemandModel(demandSettings);
        Population = new PopulationSystem(
            Map, HeatMaps, Economy.Taxes, Economy.Ledger, Demand, demandSettings, Events,
            tickInterval: Simulation.TickInterval.Slow);

        RegisterComponents();
        RegisterDefaultHeatMaps();
        RegisterSystems();

        Scheduler.InitializeSystems();
    }

    public GameSimulation() : this(GameConfig.Default)
    {
    }

    private bool _started;

    /// <summary>
    /// Announce the world is ready. Called by the host AFTER it has wired up its event
    /// observers, so the initial <see cref="CityInitializedEvent"/> isn't missed (a
    /// constructor can't publish to subscribers that don't exist yet). Idempotent.
    /// </summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        Events.Publish(new CityInitializedEvent(Config.Width, Config.Height, Config.Seed));
    }

    // --- ISimulationContext ---

    public FlowNetwork GetNetwork(NetworkType type)
    {
        if (!_networks.TryGetValue(type, out FlowNetwork? network))
        {
            network = new FlowNetwork(type);
            _networks[type] = network;
        }

        return network;
    }

    // --- Driver surface for the host ---

    /// <summary>Feed variable real time (seconds); runs the appropriate number of fixed ticks.</summary>
    public int Update(double realDeltaSeconds) => Scheduler.Update(realDeltaSeconds);

    /// <summary>Advance exactly one deterministic tick (headless runners, tests, lockstep).</summary>
    public void Step() => Scheduler.StepOnce();

    public CommandResult Submit(ICommand command) => Commands.Submit(command);

    public bool Undo() => Commands.Undo();

    public bool Redo() => Commands.Redo();

    // --- Bootstrap ---

    private void RegisterComponents()
    {
        Entities.RegisterComponent<GridPositionComponent>();
        Entities.RegisterComponent<MovementComponent>();
        Entities.RegisterComponent<BuildingComponent>();
        Entities.RegisterComponent<VehicleComponent>();
    }

    private void RegisterDefaultHeatMaps()
    {
        HeatMaps.GetOrCreate(HeatMapKind.Desirability);
        HeatMaps.GetOrCreate(HeatMapKind.LandValue);
        HeatMaps.GetOrCreate(HeatMapKind.Pollution);
        HeatMaps.GetOrCreate(HeatMapKind.Crime);
    }

    private void RegisterSystems()
    {
        // Population runs first: it refreshes the RCI demand the zoning growth rule reads below.
        Scheduler.Register(Population);

        var automata = new CellularAutomataEngine();
        automata.AddRule(new DemandGrowthRule(Demand));

        var zoning = new ZoningSystem(Map, HeatMaps, automata, Events, Random);
        Scheduler.Register(zoning);

        // Traffic movement runs every tick; it advances routed vehicles and updates congestion.
        FlowNetwork road = GetNetwork(NetworkType.Road);
        Scheduler.Register(new TrafficSystem(Entities, road, RoadCongestion, Routes, Events));

        // Utilities solve power/water coverage + capacity on a slow cadence (grids added later).
        Scheduler.Register(Utilities);

        // Economy runs after utilities each slow tick, so it sees the latest coverage reports.
        Scheduler.Register(Economy);

        // Population/demand agents and richer markets build on this in later milestones.
    }

    /// <summary>
    /// Create a utility service (power/water) over its network and register it with the utility
    /// system. Add sources/consumers to the returned grid, then it is solved each slow tick.
    /// </summary>
    public UtilityGrid CreateUtility(NetworkType kind)
    {
        var grid = new UtilityGrid(kind, GetNetwork(kind));
        Utilities.AddGrid(grid);
        return grid;
    }

    /// <summary>
    /// Build a spawner for a vehicle definition, wired to the road network, live congestion and
    /// the pooled route table. Call after the vehicle definition has been loaded. Wrap the result
    /// in a <see cref="TrafficSpawnSystem"/> and register it to generate continuous traffic.
    /// </summary>
    public VehicleSpawner CreateVehicleSpawner(string vehicleDefinitionId)
        => new(Entities, Factory, GetNetwork(NetworkType.Road), Pathfinder, RoadCongestion, Routes, Events, vehicleDefinitionId);
