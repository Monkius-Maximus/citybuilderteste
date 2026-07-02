using CityBuilder;
using CityBuilder.App;
using CityBuilder.Commands.Actions;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Presentation;
using CityBuilder.Zoning;

// =============================================================================
//  Headless driver. Proves the ENTIRE simulation runs in a plain C# console with
//  zero game-engine dependencies — the core acceptance criterion of the brief.
//  A Unity/Godot host would replace this file and pump GameSimulation.Update(dt)
//  from its frame loop, while observing the same event bus.
// =============================================================================

Console.WriteLine("== CityBuilder.Core — headless simulation ==\n");

var sim = new GameSimulation(new GameConfig(width: 32, height: 32, seed: 1337, ticksPerSecond: 10));

// --- Observer / Pub-Sub: the "UI" just watches events, never touches the sim internals ---
sim.Events.Subscribe<CityInitializedEvent>(e =>
    Console.WriteLine($"[event] City ready: {e.Width}x{e.Height}, seed {e.Seed}"));
sim.Events.Subscribe<CommandExecutedEvent>(e =>
    Console.WriteLine($"[event] {(e.Success ? "OK " : "ERR")} {e.CommandName}{(e.Message is null ? "" : " — " + e.Message)}"));

int lastDeveloped = 0;
sim.Events.Subscribe<ZoningUpdatedEvent>(e => lastDeveloped = e.DevelopedCells);

// Announce readiness now that observers are wired (see GameSimulation.Start()).
sim.Start();

// --- Factory + data-driven definitions (strictly generic identifiers) ---
var catalog = new InMemoryDefinitionSource()
    .Add(new BuildingDefinition { Id = "Residential_Standard_L1", DisplayName = "Standard Housing", Category = ZoneType.Residential, MaxOccupancy = 24 })
    .Add(new VehicleDefinition { Id = "CompactHatch_Tier1", DisplayName = "Compact Hatchback", Class = VehicleClass.Passenger, MaxSpeed = 3f, Capacity = 4 });
sim.Definitions.LoadFrom(catalog);

// --- Command pattern: every world edit goes through the processor (undoable + logged) ---
Console.WriteLine("\n-- Issuing player commands --");
SeedDesirability(sim, new GridCoord(6, 6), new GridCoord(22, 22), 1.5f);
sim.Submit(new ZoneAreaCommand(new GridCoord(8, 8), new GridCoord(20, 20), ZoneType.Residential, ZoneDensity.Medium));

// Build a straight road (a chain of connected segments) along row y = 10.
for (int x = 5; x < 15; x++)
{
    sim.Submit(new BuildRoadCommand(new GridCoord(x, 10), new GridCoord(x + 1, 10)));
}

// --- Simulation Tick Engine: run deterministic fixed ticks (framerate-independent) ---
Console.WriteLine("\n-- Running 300 fixed ticks --");
for (int i = 0; i < 300; i++)
{
    sim.Step();
}
Console.WriteLine($"Tick={sim.CurrentTick}, developed cells={lastDeveloped}, live entities={sim.Entities.AliveCount}");

// --- Pathfinding: A* over the road FlowNetwork, with and without dynamic congestion ---
Console.WriteLine("\n-- Pathfinding (A*) --");
FlowNetwork road = sim.GetNetwork(NetworkType.Road);
if (road.TryGetNodeAt(new GridCoord(5, 10), out NodeId start) &&
    road.TryGetNodeAt(new GridCoord(15, 10), out NodeId goal))
{
    var astar = new AStarPathfinder(Heuristics.Manhattan);
    var path = new List<int>();

    PathResult r = astar.FindPath(road, start.Value, goal.Value, path);
    Console.WriteLine($"A* static:     found={r.Found}, nodes={r.NodeCount}, cost={r.Cost:0.0}");

    var congestion = new CongestionWeightProvider(edgeCapacityHint: road.EdgeCount + 1);
    congestion.SetLoad(new EdgeId(0), 96f); // jam the first segment
    PathResult rc = astar.FindPath(road, start.Value, goal.Value, path, congestion);
    Console.WriteLine($"A* congested:  found={rc.Found}, nodes={rc.NodeCount}, cost={rc.Cost:0.0}  (dynamic weight raised the cost)");

    // --- Dijkstra flow field: distance-to-service for the whole network in one pass ---
    var flow = new DijkstraMap();
    flow.Build(road, new[] { start.Value });
    Console.WriteLine($"Dijkstra map:  dist({goal})={flow.GetDistance(goal.Value):0.0}, next-hop from goal = node {flow.GetNext(goal.Value)}");
}

// --- Object pooling: rent/return high-churn actors with no GC churn ---
Console.WriteLine("\n-- Object pooling --");
var pool = new CityBuilder.Common.ObjectPool<Particle>(() => new Particle(), prewarm: 4);
Particle a = pool.Rent();
Particle b = pool.Rent();
Console.WriteLine($"After 2 rents: live={pool.LiveCount}, free={pool.FreeCount}");
pool.Return(a);
pool.Return(b);
Console.WriteLine($"After 2 returns: live={pool.LiveCount}, free={pool.FreeCount}");

// --- Factory: instantiate ECS entities from definitions ---
Console.WriteLine("\n-- Factory --");
Entity house = sim.Factory.CreateBuilding("Residential_Standard_L1", new GridCoord(9, 9));
Entity car = sim.Factory.CreateVehicle("CompactHatch_Tier1", new GridCoord(5, 10));
Console.WriteLine($"Created building {house} and vehicle {car}; live entities={sim.Entities.AliveCount}");

// --- Presentation contract: procedural placeholder visuals (colored iso primitives) ---
Console.WriteLine("\n-- Procedural placeholder visuals --");
IProceduralSpriteFactory sprites = new PlaceholderSpriteFactory();
TileVisual grass = sprites.Terrain(TerrainKind.Grass, 0);
TileVisual devZone = sprites.Zone(ZoneType.Residential, ZoneDensity.Medium, development: 120);
Console.WriteLine($"grass tile  -> shape={grass.Shape}, fill={grass.Fill}");
Console.WriteLine($"grown zone  -> shape={devZone.Shape}, fill={devZone.Fill}, height={devZone.Height}");

// --- Command pattern: undo / redo on the economy contract ---
Console.WriteLine("\n-- Tax command + undo/redo (economy contract) --");
var taxes = new DemoTaxPolicy();
Console.WriteLine($"Residential tax before: {taxes.GetRate(ZoneType.Residential):0.00}");
sim.Submit(new SetTaxRateCommand(taxes, ZoneType.Residential, 0.13f));
Console.WriteLine($"After SetTax:           {taxes.GetRate(ZoneType.Residential):0.00}");
sim.Undo();
Console.WriteLine($"After Undo:             {taxes.GetRate(ZoneType.Residential):0.00}");
sim.Redo();
Console.WriteLine($"After Redo:             {taxes.GetRate(ZoneType.Residential):0.00}");

// --- Determinism: identical seed + identical inputs => identical result ---
Console.WriteLine("\n-- Determinism check --");
int runA = RunScenario(seed: 42, ticks: 300);
int runB = RunScenario(seed: 42, ticks: 300);
Console.WriteLine($"Run A developed={runA}, Run B developed={runB} -> {(runA == runB ? "DETERMINISTIC (PASS)" : "NON-DETERMINISTIC (FAIL)")}");

Console.WriteLine("\nDone.");
return;

// ------------------------------- helpers -------------------------------

static void SeedDesirability(GameSimulation sim, GridCoord min, GridCoord max, float amount)
{
    HeatMap desirability = sim.HeatMaps.GetOrCreate(HeatMapKind.Desirability);
    for (int y = min.Y; y <= max.Y; y++)
    {
        for (int x = min.X; x <= max.X; x++)
        {
            desirability.AddSource(new GridCoord(x, y), amount);
        }
    }
}

static int RunScenario(ulong seed, int ticks)
{
    var s = new GameSimulation(new GameConfig(32, 32, seed));
    SeedDesirability(s, new GridCoord(6, 6), new GridCoord(22, 22), 1.5f);
    s.Submit(new ZoneAreaCommand(new GridCoord(8, 8), new GridCoord(20, 20), ZoneType.Residential, ZoneDensity.Medium));
    for (int i = 0; i < ticks; i++)
    {
        s.Step();
    }

    int developed = 0;
    foreach (ZoneCell cell in s.Map.Zoning.AsSpan())
    {
        if (cell.IsDeveloped)
        {
            developed++;
        }
    }

    return developed;
}
