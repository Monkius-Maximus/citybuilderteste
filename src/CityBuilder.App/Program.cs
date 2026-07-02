using CityBuilder;
using CityBuilder.App;
using CityBuilder.Commands.Actions;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Presentation;
using CityBuilder.Traffic;
using CityBuilder.Zoning;

// =============================================================================
//  Headless driver. Proves the ENTIRE simulation runs in a plain C# console with
//  zero game-engine dependencies. A Unity/Godot host would replace this file and
//  pump GameSimulation.Update(dt) from its frame loop, observing the same events.
// =============================================================================

Console.WriteLine("== CityBuilder.Core — headless simulation ==\n");

var sim = new GameSimulation(new GameConfig(width: 48, height: 48, seed: 1337, ticksPerSecond: 10));

// --- Observer / Pub-Sub: the "UI" only watches events ---
sim.Events.Subscribe<CityInitializedEvent>(e =>
    Console.WriteLine($"[event] City ready: {e.Width}x{e.Height}, seed {e.Seed}"));

int spawned = 0, arrived = 0;
sim.Events.Subscribe<VehicleSpawnedEvent>(_ => spawned++);
sim.Events.Subscribe<VehicleArrivedEvent>(_ => arrived++);
int lastDeveloped = 0;
sim.Events.Subscribe<ZoningUpdatedEvent>(e => lastDeveloped = e.DevelopedCells);

sim.Start();

// --- Factory + data-driven definitions (strictly generic identifiers) ---
sim.Definitions.LoadFrom(new InMemoryDefinitionSource()
    .Add(new BuildingDefinition { Id = "Residential_Standard_L1", DisplayName = "Standard Housing", Category = ZoneType.Residential, MaxOccupancy = 24 })
    .Add(new VehicleDefinition { Id = "CompactHatch_Tier1", DisplayName = "Compact Hatchback", Class = VehicleClass.Passenger, MaxSpeed = 3f, Capacity = 4 }));

// --- Commands: zone an area (undoable) and seed its desirability so it will grow ---
Console.WriteLine("\n-- Zoning command --");
SeedDesirability(sim, new GridCoord(20, 20), new GridCoord(34, 34), 1.5f);
sim.Submit(new ZoneAreaCommand(new GridCoord(22, 22), new GridCoord(32, 32), ZoneType.Residential, ZoneDensity.Medium));

// --- Build a 9x9 road grid (gives vehicles route choices for congestion routing) ---
FlowNetwork road = sim.GetNetwork(NetworkType.Road);
RoadGridBuilder.BuildGrid(road, new GridCoord(4, 4), new GridCoord(12, 12), edgeCost: 1f, capacity: 6);
Console.WriteLine($"\n-- Road grid built: {road.NodeCount} nodes, {road.EdgeCount} directed edges --");

// --- Pathfinding: A* + a Dijkstra flow field over the grid ---
road.TryGetNodeAt(new GridCoord(4, 4), out NodeId cornerA);
road.TryGetNodeAt(new GridCoord(12, 12), out NodeId cornerB);
var path = new List<int>();
PathResult pr = sim.Pathfinder.FindPath(road, cornerA.Value, cornerB.Value, path);
Console.WriteLine($"A* {cornerA}->{cornerB}: found={pr.Found}, nodes={pr.NodeCount}, cost={pr.Cost:0.0}");

var flow = new DijkstraMap();
flow.Build(road, new[] { cornerA.Value });
Console.WriteLine($"Dijkstra flow field from {cornerA}: dist({cornerB})={flow.GetDistance(cornerB.Value):0.0}, reachable nodes seeded");

// --- Traffic: trace ONE vehicle from A to B tick by tick ---
Console.WriteLine("\n-- Single vehicle trace --");
VehicleSpawner spawner = sim.CreateVehicleSpawner("CompactHatch_Tier1");
Entity tracer = spawner.Spawn(cornerA.Value, cornerB.Value, sim.CurrentTick);
for (int step = 1; step <= 120 && sim.Entities.IsAlive(tracer); step++)
{
    sim.Step();
    if (step % 15 == 0 && sim.Entities.IsAlive(tracer))
    {
        GridPositionComponent p = sim.Entities.Get<GridPositionComponent>(tracer);
        Console.WriteLine($"  t+{step,3}: vehicle at {p.Cell}");
    }
}
Console.WriteLine($"  tracer arrived: {!sim.Entities.IsAlive(tracer)} (arrivals so far: {arrived})");

// --- Traffic: continuous spawning system (variable cadence) under congestion ---
Console.WriteLine("\n-- Continuous traffic (spawner system) --");
sim.Scheduler.Register(new TrafficSpawnSystem(spawner, sim.Routes, sim.Random, tickInterval: 3, maxActive: 24, spawnPerTick: 2));
for (int i = 0; i < 400; i++)
{
    sim.Step();
}
Console.WriteLine($"After 400 ticks: spawned={spawned}, arrived={arrived}, active={sim.Routes.ActiveCount}, developed cells={lastDeveloped}");

// --- Object pooling ---
Console.WriteLine("\n-- Object pooling --");
var pool = new CityBuilder.Common.ObjectPool<Particle>(() => new Particle(), prewarm: 4);
Particle a = pool.Rent();
Particle b = pool.Rent();
Console.WriteLine($"After 2 rents:   live={pool.LiveCount}, free={pool.FreeCount}");
pool.Return(a);
pool.Return(b);
Console.WriteLine($"After 2 returns: live={pool.LiveCount}, free={pool.FreeCount}");

// --- Presentation contract: procedural placeholder visuals ---
Console.WriteLine("\n-- Procedural placeholder visuals --");
IProceduralSpriteFactory sprites = new PlaceholderSpriteFactory();
TileVisual grownZone = sprites.Zone(ZoneType.Residential, ZoneDensity.Medium, development: 120);
Console.WriteLine($"grown residential -> shape={grownZone.Shape}, fill={grownZone.Fill}, height={grownZone.Height}");

// --- Command pattern: undo / redo on the economy contract ---
Console.WriteLine("\n-- Tax command + undo/redo (economy contract) --");
var taxes = new DemoTaxPolicy();
sim.Submit(new SetTaxRateCommand(taxes, ZoneType.Residential, 0.13f));
Console.WriteLine($"After SetTax: {taxes.GetRate(ZoneType.Residential):0.00}");
sim.Undo();
Console.WriteLine($"After Undo:   {taxes.GetRate(ZoneType.Residential):0.00}");
sim.Redo();
Console.WriteLine($"After Redo:   {taxes.GetRate(ZoneType.Residential):0.00}");

// --- Determinism: identical seed + identical inputs => identical result (incl. traffic) ---
Console.WriteLine("\n-- Determinism check (zoning + pathfinding + traffic) --");
var runA = RunScenario(seed: 42, ticks: 300);
var runB = RunScenario(seed: 42, ticks: 300);
bool same = runA == runB;
Console.WriteLine($"Run A {runA}");
Console.WriteLine($"Run B {runB}");
Console.WriteLine($"-> {(same ? "DETERMINISTIC (PASS)" : "NON-DETERMINISTIC (FAIL)")}");

Console.WriteLine("\nDone.");
return;

// ------------------------------- helpers -------------------------------

static void SeedDesirability(GameSimulation sim, GridCoord min, GridCoord max, float amount)
{
    Zoning.HeatMap desirability = sim.HeatMaps.GetOrCreate(HeatMapKind.Desirability);
    for (int y = min.Y; y <= max.Y; y++)
    {
        for (int x = min.X; x <= max.X; x++)
        {
            desirability.AddSource(new GridCoord(x, y), amount);
        }
    }
}

// Returns (developedCells, arrivedVehicles, spawnedVehicles) after a scripted run.
static (int Developed, int Arrived, int Spawned) RunScenario(ulong seed, int ticks)
{
    var s = new GameSimulation(new GameConfig(48, 48, seed));
    s.Definitions.LoadFrom(new InMemoryDefinitionSource()
        .Add(new VehicleDefinition { Id = "CompactHatch_Tier1", DisplayName = "Compact Hatchback", Class = VehicleClass.Passenger, MaxSpeed = 3f, Capacity = 4 }));

    int arrived = 0, spawned = 0;
    s.Events.Subscribe<VehicleArrivedEvent>(_ => arrived++);
    s.Events.Subscribe<VehicleSpawnedEvent>(_ => spawned++);

    RoadGridBuilder.BuildGrid(s.GetNetwork(NetworkType.Road), new GridCoord(4, 4), new GridCoord(12, 12), 1f, 6);
    SeedDesirability(s, new GridCoord(20, 20), new GridCoord(34, 34), 1.5f);
    s.Submit(new ZoneAreaCommand(new GridCoord(22, 22), new GridCoord(32, 32), ZoneType.Residential, ZoneDensity.Medium));

    VehicleSpawner spawner = s.CreateVehicleSpawner("CompactHatch_Tier1");
    s.Scheduler.Register(new TrafficSpawnSystem(spawner, s.Routes, s.Random, tickInterval: 3, maxActive: 24, spawnPerTick: 2));

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

    return (developed, arrived, spawned);
}
