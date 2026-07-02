using CityBuilder.Commands;
using CityBuilder.Common;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Simulation;
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
        var automata = new CellularAutomataEngine();
        automata.AddRule(new ZoneGrowthRule());

        var zoning = new ZoningSystem(Map, HeatMaps, automata, Events, Random);
        Scheduler.Register(zoning);

        // Traffic, utilities, population and economy systems register here in later milestones,
        // each choosing its own TickInterval. The scheduler already supports variable cadences.
    }
}
