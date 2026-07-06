using CityBuilder;
using CityBuilder.App;
using CityBuilder.Commands.Actions;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Economy;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Persistence;
using CityBuilder.Population;
using CityBuilder.Presentation;
using CityBuilder.Shell;
using CityBuilder.Simulation;
using CityBuilder.Traffic;
using CityBuilder.Utilities;
using CityBuilder.Zoning;

// =============================================================================
//  Headless driver. Proves the ENTIRE simulation runs in a plain C# console with
//  zero game-engine dependencies. A Unity/Godot host would replace this file and
//  pump GameSimulation.Update(dt) from its frame loop, observing the same events.
// =============================================================================

Console.WriteLine($"== {GameInfo.Kicker} {GameInfo.Title} — {GameInfo.Tagline} (headless) ==");
Console.WriteLine(GameInfo.FooterLine(10) + "\n");

var sim = new GameSimulation(new GameConfig(width: 48, height: 48, seed: 1337, ticksPerSecond: 10));

// --- Observer / Pub-Sub: the "UI" only watches events ---
sim.Events.Subscribe<CityInitializedEvent>(e =>
    Console.WriteLine($"[event] City ready: {e.Width}x{e.Height}, seed {e.Seed}"));

int spawned = 0, arrived = 0;
sim.Events.Subscribe<VehicleSpawnedEvent>(_ => spawned++);
sim.Events.Subscribe<VehicleArrivedEvent>(_ => arrived++);
int lastDeveloped = 0;
sim.Events.Subscribe<ZoningUpdatedEvent>(e => lastDeveloped = e.DevelopedCells);

// Economy snapshots (captured from events; printed later).
Money ecoBalance = Money.Zero, ecoIncome = Money.Zero, ecoExpenses = Money.Zero;
long ecoTick = 0, ecoPopulation = 0;
sim.Events.Subscribe<BudgetChangedEvent>(e =>
{
    ecoBalance = e.Balance; ecoIncome = e.Income; ecoExpenses = e.Expenses;
    ecoTick = e.Tick; ecoPopulation = e.Population;
});
Money labourPrice = Money.Zero, goodsPrice = Money.Zero;
sim.Events.Subscribe<MarketClearedEvent>(e =>
{
    if (e.Resource == ResourceKind.Labour) labourPrice = e.Price;
    else if (e.Resource == ResourceKind.Goods) goodsPrice = e.Price;
});

sim.Start();

// --- Factory + data-driven definitions (strictly generic identifiers) ---
sim.Definitions.LoadFrom(DemoDefinitions());

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

// --- Utilities: power coverage via Dijkstra flow field + capacity brownout ---
Console.WriteLine("\n-- Utilities (power grid) --");
FlowNetwork powerNet = sim.GetNetwork(NetworkType.PowerLine);
RoadGridBuilder.BuildGrid(powerNet, new GridCoord(4, 4), new GridCoord(9, 9), edgeCost: 1f, capacity: 9999);
UtilityGrid powerGrid = sim.CreateUtility(NetworkType.PowerLine);

powerNet.TryGetNodeAt(new GridCoord(4, 4), out NodeId plant);
powerGrid.AddSource(plant, capacity: 40f);
powerGrid.MaxServiceDistance = 8f;
foreach (GridCoord c in new[] { new GridCoord(5, 5), new GridCoord(6, 6), new GridCoord(7, 7), new GridCoord(9, 9) })
{
    powerNet.TryGetNodeAt(c, out NodeId consumer);
    powerGrid.AddConsumer(consumer, demand: 15f);
}
NodeId offGrid = powerNet.AddNode(new GridCoord(40, 40)); // not wired to the network
powerGrid.AddConsumer(offGrid, demand: 15f);

sim.Events.Subscribe<UtilityUpdatedEvent>(e =>
    Console.WriteLine($"  [{e.Kind}] supply={e.Supply:0}, reachDemand={e.ReachableDemand:0}, served={e.ServedDemand:0} ({e.ServedConsumers}/{e.ReachableConsumers}), brownout={e.Brownout}"));

UtilityReport under = powerGrid.Solve();
powerNet.TryGetNodeAt(new GridCoord(9, 9), out NodeId farNode);
float farDist = powerGrid.CoverageDistance(farNode);
bool offGridReachable = !float.IsPositiveInfinity(powerGrid.CoverageDistance(offGrid));
Console.WriteLine($"  supply 40: served {under.ServedConsumers}/{under.ReachableConsumers} reachable, brownout={under.Brownout} " +
                  $"[(9,9) dist={farDist:0} > range 8 -> excluded; (40,40) off-grid reachable={offGridReachable}]");

powerGrid.ClearSources();
powerGrid.AddSource(plant, capacity: 70f);
powerGrid.MaxServiceDistance = 12f;
UtilityReport upgraded = powerGrid.Solve();
Console.WriteLine($"  supply 70 + range 12: served {upgraded.ServedConsumers}/{upgraded.ReachableConsumers} reachable, brownout={upgraded.Brownout}");

// Let the UtilitySystem solve on its own slow tick and emit the observer event (line above is a direct call).
for (int i = 0; i < 60; i++)
{
    sim.Step();
}

// --- Economy: taxes on zones + utility billing - infrastructure upkeep -> treasury ---
Console.WriteLine("\n-- Economy (treasury / markets) --");
Console.WriteLine($"  settle @tick {ecoTick}: balance={ecoBalance}, income={ecoIncome}, expenses={ecoExpenses}, population~{ecoPopulation}");
Console.WriteLine($"  market prices: labour={labourPrice}, goods={goodsPrice}");

// --- Population & RCI demand: the growth loop (zones -> people -> demand -> growth) ---
Console.WriteLine("\n-- Population & RCI demand --");
DemandModel demand = sim.Demand;
Console.WriteLine($"  population {demand.Population:N0}, jobs {demand.Jobs:N0}, employment {demand.EmploymentRate:P0}");
Console.WriteLine($"  demand  R {demand.Residential:+0.00;-0.00} · C {demand.Commercial:+0.00;-0.00} · I {demand.Industrial:+0.00;-0.00}");
Console.WriteLine($"  sector balances: households {sim.Population.Sectors.Households.Balance}, " +
                  $"commerce {sim.Population.Sectors.Commerce.Balance}, industry {sim.Population.Sectors.Industry.Balance}");

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

// --- Command pattern: a tax command that actually drives the economy, with undo ---
Console.WriteLine("\n-- Tax command (drives the economy) + undo --");
ITaxPolicy taxPolicy = sim.Economy.Taxes;
Console.WriteLine($"Residential tax {taxPolicy.GetRate(ZoneType.Residential):0.00}, income {ecoIncome}, balance {ecoBalance}");
sim.Submit(new SetTaxRateCommand(taxPolicy, ZoneType.Residential, 0.20f));
for (int i = 0; i < 60; i++) sim.Step(); // let the economy re-settle at the new rate
Console.WriteLine($"After SetTax 0.20 + settle: rate {taxPolicy.GetRate(ZoneType.Residential):0.00}, income {ecoIncome}");
sim.Undo();
for (int i = 0; i < 60; i++) sim.Step();
Console.WriteLine($"After Undo + settle:        rate {taxPolicy.GetRate(ZoneType.Residential):0.00}, income {ecoIncome}");

// --- Persistence: binary snapshot save -> fresh sim -> load -> identical checksum ---
Console.WriteLine("\n-- Persistence: binary snapshot save/load --");
var saveStream = new MemoryStream();
SaveGame.Write(sim, saveStream);
ulong checksumLive = StateChecksum.Compute(sim);

saveStream.Position = 0;
GameConfig savedConfig = SaveGame.ReadConfig(saveStream);
var loaded = new GameSimulation(savedConfig);
loaded.Definitions.LoadFrom(DemoDefinitions()); // bootstrap = definitions/systems only; world content comes from the save
saveStream.Position = 0;
SaveGame.ReadInto(loaded, saveStream);

ulong checksumLoaded = StateChecksum.Compute(loaded);
Console.WriteLine($"  snapshot: {saveStream.Length} bytes; live={checksumLive:X16} loaded={checksumLoaded:X16} -> {(checksumLive == checksumLoaded ? "MATCH (PASS)" : "MISMATCH (FAIL)")}");
Console.WriteLine($"  restored: tick={loaded.CurrentTick}, treasury={loaded.Economy.Balance}, buildings={loaded.Entities.AliveCount}, " +
                  $"road nodes={loaded.GetNetwork(NetworkType.Road).NodeCount} (vehicles are transient by design)");

// --- Replay: record commands (with ticks), serialize the log, re-run -> identical checksum ---
Console.WriteLine("\n-- Replay: serialized command log reproduces the exact state --");
var codec = CommandCodec.CreateDefault();
(ulong liveChecksum, byte[] logBytes, long finalTick, int entryCount) = RecordedRun(codec, seed: 99);
ulong replayChecksum = ReplayRun(codec, logBytes, seed: 99, finalTick);
Console.WriteLine($"  log: {entryCount} entries, {logBytes.Length} bytes on the wire (zone, tax raise, undo)");
Console.WriteLine($"  live={liveChecksum:X16} replay={replayChecksum:X16} -> {(liveChecksum == replayChecksum ? "MATCH (PASS)" : "MISMATCH (FAIL)")}");

// --- Game shell: the "Aegean Marble" pre-game flow, headless (design handoff option 1a) ---
Console.WriteLine("\n-- Game shell: Found a New City -> save -> Load City list -> Settings --");

var shell = new GameShell(newCityForm: new NewCityForm(randomizerSeed: 7));
shell.ScreenChanged += s => Console.WriteLine($"  [shell] screen -> {s}");

// Found a New City — the exact values from the approved mockup.
shell.OpenNewCity();
shell.NewCity.CityName = "Nova Polis";
shell.NewCity.MapSize = MapSizePreset.Township; // 128 x 128
shell.NewCity.SeedText = "314159";
shell.NewCity.Terrain = TerrainPreset.VerdantPlains;
GameConfig foundedConfig = shell.FoundCity();
Console.WriteLine($"  founded '{foundedConfig.CityName}': {foundedConfig.Width}x{foundedConfig.Height}, seed {foundedConfig.Seed}, {foundedConfig.Terrain}");

// World generation per terrain preset (deterministic; same seed => same map).
var novaPolis = new GameSimulation(foundedConfig);
novaPolis.Definitions.LoadFrom(DemoDefinitions());
TerrainGenerator.Generate(novaPolis.Map.Terrain, foundedConfig.Seed, foundedConfig.Terrain);
Console.WriteLine($"  terrain census: {TerrainCensus(novaPolis)}");

var deltaPreview = new GameSimulation(new GameConfig(128, 128, foundedConfig.Seed, 10, "Delta Preview", TerrainPreset.RiverDelta));
TerrainGenerator.Generate(deltaPreview.Map.Terrain, foundedConfig.Seed, TerrainPreset.RiverDelta);
Console.WriteLine($"  (river delta, same seed): {TerrainCensus(deltaPreview)}");

// Grow the new city a little so its save has real numbers on the Load screen.
SeedDesirability(novaPolis, new GridCoord(40, 40), new GridCoord(70, 70), 1.5f);
novaPolis.Submit(new ZoneAreaCommand(new GridCoord(44, 44), new GridCoord(64, 64), ZoneType.Residential, ZoneDensity.Medium));
for (int i = 0; i < 300; i++) novaPolis.Step();
Console.WriteLine($"  after 300 ticks: population~{ZoningStats.Population(novaPolis.Map.Zoning)}, treasury {novaPolis.Economy.Balance}, {GameCalendar.Describe(novaPolis.CurrentTick)}");

// Save two cities and list them the way the Load City screen does.
string savesDir = Path.Combine(Path.GetTempPath(), "polis-saves");
Directory.CreateDirectory(savesDir);
using (FileStream f = File.Create(Path.Combine(savesDir, "nova-polis" + SaveCatalog.Extension)))
{
    SaveGame.Write(novaPolis, f);
}

var portoVerde = new GameSimulation(new GameConfig(64, 64, 271828, 10, "Porto Verde", TerrainPreset.CoastalReach));
portoVerde.Definitions.LoadFrom(DemoDefinitions());
TerrainGenerator.Generate(portoVerde.Map.Terrain, 271828, TerrainPreset.CoastalReach);
for (int i = 0; i < 120; i++) portoVerde.Step();
using (FileStream f = File.Create(Path.Combine(savesDir, "porto-verde" + SaveCatalog.Extension)))
{
    SaveGame.Write(portoVerde, f);
}

Console.WriteLine("  Load City:");
DateTime nowUtc = DateTime.UtcNow;
foreach (SaveSlot slot in SaveCatalog.Scan(savesDir))
{
    Console.WriteLine($"    {slot.Metadata.CityName,-12} {slot.Metadata.Describe(),-52} {RelativeTime.Describe(slot.Metadata.SavedAtUtc, nowUtc)}   [LOAD]");
}

// Settings screen semantics: BACK discards, APPLY commits; then a persistence round-trip.
shell.ExitToTitle();
shell.OpenSettings();
shell.Settings.MusicVolume = 25;
shell.Back(); // discard
Console.WriteLine($"  settings after BACK: music={shell.Settings.MusicVolume} (discarded)");
shell.OpenSettings();
shell.Settings.MusicVolume = 25;
shell.ApplySettings(); // commit
var settingsBlob = new MemoryStream();
shell.Settings.Save(settingsBlob);
settingsBlob.Position = 0;
GameSettings reloadedSettings = GameSettings.Load(settingsBlob);
Console.WriteLine($"  settings after APPLY + reload: music={reloadedSettings.MusicVolume}, autosave={reloadedSettings.Autosave}, uiScale={reloadedSettings.UiScale}%");

// --- Determinism: identical seed + identical inputs => identical result (incl. traffic) ---
Console.WriteLine("\n-- Determinism check (zoning + pathfinding + traffic + utilities + economy + population) --");
var runA = RunScenario(seed: 42, ticks: 300);
var runB = RunScenario(seed: 42, ticks: 300);
bool same = runA == runB;
Console.WriteLine($"Run A {runA}");
Console.WriteLine($"Run B {runB}");
Console.WriteLine($"-> {(same ? "DETERMINISTIC (PASS)" : "NON-DETERMINISTIC (FAIL)")}");

Console.WriteLine("\nDone.");
return;

// ------------------------------- helpers -------------------------------

// The demo's data catalog (strictly generic identifiers, per the legal constraint).
static InMemoryDefinitionSource DemoDefinitions() => new InMemoryDefinitionSource()
    .Add(new BuildingDefinition { Id = "Residential_Standard_L1", DisplayName = "Standard Housing", Category = ZoneType.Residential, MaxOccupancy = 24 })
    .Add(new VehicleDefinition { Id = "CompactHatch_Tier1", DisplayName = "Compact Hatchback", Class = VehicleClass.Passenger, MaxSpeed = 3f, Capacity = 4 });

// Tile-kind counts for a quick look at what a terrain preset produced.
static string TerrainCensus(GameSimulation sim)
{
    int grass = 0, water = 0, forest = 0, sand = 0, rock = 0;
    foreach (TerrainCell cell in sim.Map.Terrain.AsSpan())
    {
        switch (cell.Kind)
        {
            case TerrainKind.Water: water++; break;
            case TerrainKind.Forest: forest++; break;
            case TerrainKind.Sand: sand++; break;
            case TerrainKind.Rock: rock++; break;
            default: grass++; break;
        }
    }

    return $"grass {grass}, forest {forest}, water {water}, sand {sand}, rock {rock}";
}

// Deterministic bootstrap shared by the recorded session and its replay: same config/seed,
// same definitions, same scenario content, same systems. Only the COMMANDS differ, and those
// come from the log.
static GameSimulation BuildReplayScenario(ulong seed)
{
    var s = new GameSimulation(new GameConfig(48, 48, seed));
    s.Definitions.LoadFrom(DemoDefinitions());
    RoadGridBuilder.BuildGrid(s.GetNetwork(NetworkType.Road), new GridCoord(4, 4), new GridCoord(12, 12), 1f, 6);
    SeedDesirability(s, new GridCoord(20, 20), new GridCoord(34, 34), 1.5f);
    VehicleSpawner sp = s.CreateVehicleSpawner("CompactHatch_Tier1");
    s.Scheduler.Register(new TrafficSpawnSystem(sp, s.Routes, s.Random, tickInterval: 3, maxActive: 24, spawnPerTick: 2));
    return s;
}

// A live session with a recorder attached: zone at tick 0, raise taxes at tick 120, undo at
// tick 180. Returns the final checksum plus the serialized command log.
static (ulong Checksum, byte[] LogBytes, long FinalTick, int Entries) RecordedRun(CommandCodec codec, ulong seed)
{
    GameSimulation s = BuildReplayScenario(seed);
    var log = new ReplayLog();
    s.Commands.Recorder = new ReplayRecorder(log);

    s.Submit(new ZoneAreaCommand(new GridCoord(22, 22), new GridCoord(32, 32), ZoneType.Residential, ZoneDensity.Medium));
    for (int i = 0; i < 240; i++)
    {
        s.Step();
        if (s.CurrentTick == 120)
        {
            s.Submit(new SetTaxRateCommand(s.Economy.Taxes, ZoneType.Residential, 0.15f));
        }

        if (s.CurrentTick == 180)
        {
            s.Undo();
        }
    }

    var wire = new MemoryStream();
    codec.WriteLog(log, wire);
    return (StateChecksum.Compute(s), wire.ToArray(), s.CurrentTick, log.Count);
}

// Replay from the serialized bytes into a freshly bootstrapped scenario.
static ulong ReplayRun(CommandCodec codec, byte[] logBytes, ulong seed, long finalTick)
{
    GameSimulation s = BuildReplayScenario(seed);
    ReplayLog log = codec.ReadLog(new MemoryStream(logBytes), s);
    ReplayPlayer.Play(s, log, finalTick);
    return StateChecksum.Compute(s);
}

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

// Returns (developedCells, arrivedVehicles, spawnedVehicles, powerServed, treasuryUnits, population) after a scripted run.
static (int Developed, int Arrived, int Spawned, int PowerServed, long Treasury, long Population) RunScenario(ulong seed, int ticks)
{
    var s = new GameSimulation(new GameConfig(48, 48, seed));
    s.Definitions.LoadFrom(DemoDefinitions());

    int arrived = 0, spawned = 0, powerServed = 0;
    long treasury = 0, population = 0;
    s.Events.Subscribe<VehicleArrivedEvent>(_ => arrived++);
    s.Events.Subscribe<VehicleSpawnedEvent>(_ => spawned++);
    s.Events.Subscribe<UtilityUpdatedEvent>(e => powerServed = e.ServedConsumers);
    s.Events.Subscribe<BudgetChangedEvent>(e => treasury = e.Balance.Units);
    s.Events.Subscribe<PopulationChangedEvent>(e => population = e.Population);

    RoadGridBuilder.BuildGrid(s.GetNetwork(NetworkType.Road), new GridCoord(4, 4), new GridCoord(12, 12), 1f, 6);
    SeedDesirability(s, new GridCoord(20, 20), new GridCoord(34, 34), 1.5f);
    s.Submit(new ZoneAreaCommand(new GridCoord(22, 22), new GridCoord(32, 32), ZoneType.Residential, ZoneDensity.Medium));

    VehicleSpawner spawner = s.CreateVehicleSpawner("CompactHatch_Tier1");
    s.Scheduler.Register(new TrafficSpawnSystem(spawner, s.Routes, s.Random, tickInterval: 3, maxActive: 24, spawnPerTick: 2));

    // A power service: source + several consumers, solved each slow tick by the UtilitySystem.
    FlowNetwork powerNet = s.GetNetwork(NetworkType.PowerLine);
    RoadGridBuilder.BuildGrid(powerNet, new GridCoord(4, 4), new GridCoord(8, 8), 1f, 9999);
    UtilityGrid power = s.CreateUtility(NetworkType.PowerLine);
    powerNet.TryGetNodeAt(new GridCoord(4, 4), out NodeId src);
    power.AddSource(src, 40f);
    foreach (GridCoord c in new[] { new GridCoord(5, 5), new GridCoord(6, 6), new GridCoord(7, 7) })
    {
        powerNet.TryGetNodeAt(c, out NodeId consumer);
        power.AddConsumer(consumer, 15f);
    }

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

    return (developed, arrived, spawned, powerServed, treasury, population);
}
