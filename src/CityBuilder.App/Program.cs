using CityBuilder;
using CityBuilder.App;
using CityBuilder.Commands.Actions;
using CityBuilder.Data;
using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Economy;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Library;
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

// City-management CLI (M2): `list | export <city> <out.polispack> | import <file>`.
// Any argument switches from the demo to the tool; proves the library is engine-agnostic.
if (args.Length > 0)
{
    RunCli(args);
    return;
}

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

// --- City Library (M1): save through the manager, then the full CRUD lifecycle ---
Console.WriteLine("\n-- City Library: save / list / rename / duplicate / delete / autosave --");
string savesDir = Path.Combine(Path.GetTempPath(), "polis-saves");
if (Directory.Exists(savesDir))
{
    Directory.Delete(savesDir, recursive: true); // fresh demo run, deterministic listing
}

var library = new CityLibrary(savesDir);
int libraryChanges = 0;
library.LibraryChanged += () => libraryChanges++;

CitySlot novaSlot = library.Save(novaPolis);

var portoVerde = new GameSimulation(new GameConfig(64, 64, 271828, 10, "Porto Verde", TerrainPreset.CoastalReach));
portoVerde.Definitions.LoadFrom(DemoDefinitions());
TerrainGenerator.Generate(portoVerde.Map.Terrain, 271828, TerrainPreset.CoastalReach);
for (int i = 0; i < 120; i++) portoVerde.Step();
CitySlot portoSlot = library.Save(portoVerde);
Console.WriteLine($"  saved '{novaSlot.CityName}' -> {novaSlot.FileName} and '{portoSlot.CityName}' -> {portoSlot.FileName}");
Console.WriteLine($"  SaveCatalog façade sees {SaveCatalog.Scan(savesDir).Count} save(s)");

PrintLibrary("after saving 2 cities", library);

// Rename rewrites the name inside the save WITHOUT loading the world or renaming the file.
CitySlot renamed = library.Rename(novaSlot, "Nova Polis Prime");
Console.WriteLine($"  renamed: '{renamed.CityName}' (file kept: {renamed.FileName})");

// Duplicate + delete: the copy goes to .trash/, recoverable against accidental clicks.
CitySlot copy = library.Duplicate(renamed, "Nova Polis II");
library.Delete(copy);
Console.WriteLine($"  duplicated + deleted: trash now holds {library.TrashContents().Count} file(s)");

// Loading through the library = same rebuild rule (bootstrap first, then restore).
GameSimulation reloadedCity = library.Load(renamed, s => s.Definitions.LoadFrom(DemoDefinitions()));
Console.WriteLine($"  reloaded '{reloadedCity.Config.CityName}': tick={reloadedCity.CurrentTick}, checksum match={StateChecksum.Compute(reloadedCity) == StateChecksum.Compute(novaPolis)}");

// Autosave: 5-slot rotation driven by the Settings interval (we control elapsed time here).
var autosaver = new AutosaveService(library, shell.Settings, rotationSlots: 5);
for (int i = 0; i < 7; i++)
{
    autosaver.Update(novaPolis, TimeSpan.FromMinutes(11)); // > Every10Min default => fires each call
}
IReadOnlyList<CitySlot> autosaves = autosaver.SlotsFor(novaPolis);
Console.WriteLine($"  autosave: 7 fired -> {autosaves.Count} slots kept (oldest overwritten), e.g. {autosaves[0].FileName}");
Console.WriteLine($"  library change events observed: {libraryChanges}");

PrintLibrary("final Load City screen", library);

// --- Founding codes (M2): shareable seeds in both formats, integrity-checked ---
Console.WriteLine("\n-- Founding codes (shareable seeds) --");
string readableCode = FoundingCode.EncodeReadable(foundedConfig);
string compactCode = FoundingCode.EncodeCompact(foundedConfig);
Console.WriteLine($"  readable: {readableCode}");
Console.WriteLine($"  compact:  {compactCode}");

FoundingCode.TryDecode(readableCode, out GameConfig fromReadable, out _);
FoundingCode.TryDecode(compactCode, out GameConfig fromCompact, out _);

// Bit-identical world regeneration: terrain census from the decoded config matches the original.
string originalCensus = TerrainCensus(novaPolis);
var regen = new GameSimulation(fromReadable);
TerrainGenerator.Generate(regen.Map.Terrain, fromReadable.Seed, fromReadable.Terrain);
Console.WriteLine($"  decoded readable -> T{fromReadable.Width} {fromReadable.Terrain} seed {fromReadable.Seed}; terrain matches original: {TerrainCensus(regen) == originalCensus}");
Console.WriteLine($"  compact decodes to the same setup: {fromCompact.Seed == fromReadable.Seed && fromCompact.Terrain == fromReadable.Terrain && fromCompact.Width == fromReadable.Width}");

bool mistypeRejected = !FoundingCode.TryDecode("POLIS-T128-VP-314159-ZZ", out _, out string? codeError);
Console.WriteLine($"  mistyped code rejected: {mistypeRejected} ({codeError})");

// --- City package (M2): portable .polispack with an integrity check ---
Console.WriteLine("\n-- City package (.polispack) export / import --");
var packStream = new MemoryStream();
CityPackage.Export(renamed, packStream);
byte[] packBytes = packStream.ToArray();
Console.WriteLine($"  exported '{renamed.CityName}' -> {packBytes.Length} bytes");

byte[] tampered = (byte[])packBytes.Clone();
tampered[^1] ^= 0xFF; // flip a payload byte
ImportResult badImport = CityPackage.Import(new MemoryStream(tampered), library);
Console.WriteLine($"  tampered import: {badImport.Status} — {badImport.Message}");

ImportResult goodImport = CityPackage.Import(new MemoryStream(packBytes), library);
Console.WriteLine($"  clean import: {goodImport.Status} — {goodImport.Message} (name-collision suffix applied)");

// --- Save thumbnails (M3, save v3): real minimaps stored in the save metadata ---
Console.WriteLine("\n-- Save thumbnail (v3) --");
SaveMetadata meta = renamed.Metadata;
Console.WriteLine($"  '{meta.CityName}': thumbnail {meta.ThumbnailWidth}x{meta.ThumbnailHeight}, {meta.Thumbnail.Length} bytes, present={meta.HasThumbnail}");
Console.WriteLine("  reader accepts save versions 2..3 (older saves load fine, just without a thumbnail)");
if (meta.HasThumbnail)
{
    Console.WriteLine("  minimap preview (~ water · & forest · . grass · + built · # dense):");
    PrintThumbnailAscii(meta.Thumbnail, meta.ThumbnailWidth, meta.ThumbnailHeight, cols: 56, rows: 12);
}

// --- M4: the whole pre-game flow driven through GameShell + GameHost (as a frontend would) ---
Console.WriteLine("\n-- Pre-game flow via GameShell + GameHost (end to end) --");
string hostDir = Path.Combine(Path.GetTempPath(), "polis-host");
if (Directory.Exists(hostDir)) Directory.Delete(hostDir, recursive: true);

var hostShell = new GameShell();
var host = new GameHost(hostShell, new CityLibrary(hostDir), new GameSettings(), s => s.Definitions.LoadFrom(DemoDefinitions()));
hostShell.ScreenChanged += s => Console.WriteLine($"  [shell] -> {s}");
host.ActiveChanged += s => Console.WriteLine($"  [host] active city: '{s.Config.CityName}' ({s.Config.Width}x{s.Config.Height}, {s.Config.Terrain})");

// Found a city entirely through the menu, then let the host loop drive real-time ticks.
hostShell.OpenNewCity();
hostShell.NewCity.CityName = "Aurelia";
hostShell.NewCity.MapSize = MapSizePreset.Hamlet;
hostShell.NewCity.SeedText = "2024";
hostShell.NewCity.Terrain = TerrainPreset.Highlands;
hostShell.FoundCity();                       // -> GameHost.NewCity: builds world + terrain, sets Active
for (int i = 0; i < 20; i++) host.Tick(0.1); // 2s of frame time -> fixed ticks + autosave check
host.SaveActive();
Console.WriteLine($"  saved active -> '{host.ActiveSlot?.CityName}' ({host.ActiveSlot?.FileName})");

// Found + save a second city, then LOAD the first back via the Load screen.
hostShell.Back();
hostShell.OpenNewCity();
hostShell.NewCity.CityName = "Belport";
hostShell.NewCity.MapSize = MapSizePreset.Hamlet;
hostShell.NewCity.SeedText = "555";
hostShell.NewCity.Terrain = TerrainPreset.CoastalReach;
hostShell.FoundCity();
host.SaveActive();

hostShell.ExitToTitle();
hostShell.OpenLoadCity();
CitySlot aurelia = host.Library.Refresh().First(x => x.CityName == "Aurelia");
hostShell.LoadCity(aurelia);                 // -> GameHost.LoadSlot: rebuilds Active from the save

// Rename, duplicate, delete (to trash) and export -> import, all through the host.
CitySlot renamedHost = host.RenameSlot(aurelia, "Aurelia Magna");
CitySlot dup = host.DuplicateSlot(renamedHost, "Aurelia Copy");
host.DeleteSlot(dup);
var pkg = new MemoryStream();
host.ExportSlot(renamedHost, pkg);
pkg.Position = 0;
ImportResult hostImport = host.ImportPackage(pkg);
Console.WriteLine($"  rename/dup/delete/export/import -> import {hostImport.Status} as '{hostImport.Slot?.CityName}', trash holds {host.Library.TrashContents().Count}");

Console.WriteLine("  final library:");
foreach (CitySlot s in host.Library.Refresh())
{
    Console.WriteLine($"    {s.CityName,-22} {GameCalendar.Describe(s.Metadata.Tick),-16} thumb={s.Metadata.HasThumbnail}");
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

// City-management command-line tool. Operates on a fixed library folder so `export`/`import`
// are a real round-trip you can run across machines.
static void RunCli(string[] args)
{
    string dir = Path.Combine(Path.GetTempPath(), "polis-cli");
    var library = new CityLibrary(dir);
    string verb = args[0].ToLowerInvariant();

    switch (verb)
    {
        case "list":
        {
            IReadOnlyList<CitySlot> slots = library.Refresh();
            if (slots.Count == 0)
            {
                Console.WriteLine($"No cities in {dir}");
                break;
            }

            foreach (CitySlot slot in slots)
            {
                string badge = slot.IsAutosave ? " [AUTO]" : "";
                Console.WriteLine($"{slot.CityName,-20} {slot.Metadata.Describe()}  ({slot.FileName}){badge}");
            }

            break;
        }

        case "export":
        {
            if (args.Length < 3)
            {
                Console.WriteLine("usage: export <city-name-or-file> <out.polispack>");
                break;
            }

            CitySlot? match = FindSlot(library, args[1]);
            if (match is null)
            {
                Console.WriteLine($"City not found: {args[1]}");
                break;
            }

            using FileStream f = File.Create(args[2]);
            CityPackage.Export(match.Value, f);
            Console.WriteLine($"Exported '{match.Value.CityName}' -> {args[2]}");
            break;
        }

        case "import":
        {
            if (args.Length < 2)
            {
                Console.WriteLine("usage: import <file.polispack>");
                break;
            }

            using FileStream f = File.OpenRead(args[1]);
            ImportResult result = CityPackage.Import(f, library);
            Console.WriteLine($"{result.Status}: {result.Message}");
            break;
        }

        default:
            Console.WriteLine("verbs: list | export <city> <out.polispack> | import <file.polispack>");
            break;
    }
}

// Match a library slot by city name (case-insensitive) or exact file name.
static CitySlot? FindSlot(CityLibrary library, string query)
{
    foreach (CitySlot slot in library.Refresh())
    {
        if (string.Equals(slot.CityName, query, StringComparison.OrdinalIgnoreCase)
            || string.Equals(slot.FileName, query, StringComparison.OrdinalIgnoreCase))
        {
            return slot;
        }
    }

    return null;
}

// Rough ASCII view of a stored RGBA thumbnail — proves it's a real minimap, headless.
static void PrintThumbnailAscii(byte[] rgba, int width, int height, int cols, int rows)
{
    for (int ry = 0; ry < rows; ry++)
    {
        Console.Write("    ");
        int py = ry * height / rows;
        for (int cx = 0; cx < cols; cx++)
        {
            int px = cx * width / cols;
            int i = (py * width + px) * 4;
            int r = rgba[i], g = rgba[i + 1], b = rgba[i + 2];

            char ch;
            if (b > r + 30 && b > g + 30)
            {
                ch = '~'; // water-ish (blue dominant)
            }
            else
            {
                int brightness = (r + g + b) / 3;
                ch = brightness < 90 ? '&' : brightness < 140 ? '.' : brightness < 190 ? '+' : '#';
            }

            Console.Write(ch);
        }

        Console.Write('\n');
    }
}

// Render the library the way the Load City screen presents it (badge on autosaves).
static void PrintLibrary(string label, CityLibrary library)
{
    Console.WriteLine($"  {label}:");
    DateTime nowUtc = DateTime.UtcNow;
    foreach (CitySlot slot in library.Refresh())
    {
        string badge = slot.IsAutosave ? " [AUTO]" : "";
        Console.WriteLine($"    {slot.CityName,-18} {slot.Metadata.Describe(),-50} {RelativeTime.Describe(slot.Metadata.SavedAtUtc, nowUtc)}   [LOAD]{badge}");
    }
}

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
