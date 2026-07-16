using CityBuilder.Grid;
using CityBuilder.Library;

namespace CityBuilder.Shell;

/// <summary>
/// The reusable, engine-agnostic controller that wires the menu state machine
/// (<see cref="GameShell"/>) to the city library and the live simulation lifecycle. A frontend
/// creates one of these, subscribes its renderer to <see cref="ActiveChanged"/> and the event
/// bus, and drives it from UI buttons — it owns no rendering and no flow logic of its own, so
/// the same host runs under Unity, Godot or the console.
/// <para>
/// It closes the city-management loop: founding builds a fresh world (bootstrap + terrain gen),
/// loading rebuilds from a save, and rename/duplicate/delete/export/import all route through the
/// <see cref="Library"/>. Autosave ticks alongside the simulation.
/// </para>
/// </summary>
public sealed class GameHost
{
    private readonly Action<GameSimulation> _bootstrap;

    /// <param name="bootstrap">
    /// The SAME setup a new game and a load both need — register definitions and scenario
    /// systems, but NOT world content (terrain/zoning come from generation or the save).
    /// </param>
    public GameHost(GameShell shell, CityLibrary library, GameSettings settings, Action<GameSimulation> bootstrap)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        Library = library ?? throw new ArgumentNullException(nameof(library));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
        Autosave = new AutosaveService(library, settings);

        // Wire the shell's intents that need no extra input. Rename/export/import need a name or
        // a file path from the UI, so the frontend calls the matching method after collecting it.
        Shell.CityFounded += config => NewCity(config);
        Shell.LoadRequested += slot => LoadSlot(slot);
        Shell.DeleteRequested += slot => DeleteSlot(slot);
        Shell.ContinueRequested += () => ContinueMostRecent();
    }

    public GameShell Shell { get; }
    public CityLibrary Library { get; }
    public GameSettings Settings { get; }
    public AutosaveService Autosave { get; }

    /// <summary>The city currently being played (null on the menu before a city is chosen).</summary>
    public GameSimulation? Active { get; private set; }

    /// <summary>The library slot the active city came from (null for a brand-new, unsaved city).</summary>
    public CitySlot? ActiveSlot { get; private set; }

    /// <summary>Raised when a new city becomes active (founded or loaded) — rebind the view here.</summary>
    public event Action<GameSimulation>? ActiveChanged;

    // --- Lifecycle ---

    /// <summary>FOUND CITY: build a fresh world from the config (bootstrap + procedural terrain).</summary>
    public GameSimulation NewCity(GameConfig config)
    {
        var sim = new GameSimulation(config);
        _bootstrap(sim);
        TerrainGenerator.Generate(sim.Map.Terrain, config.Seed, config.Terrain);
        SetActive(sim, slot: null);
        return sim;
    }

    /// <summary>LOAD: rebuild a city from a save slot (bootstrap first, then restore).</summary>
    public GameSimulation LoadSlot(in CitySlot slot)
    {
        GameSimulation sim = Library.Load(slot, _bootstrap);
        SetActive(sim, slot);
        return sim;
    }

    /// <summary>CONTINUE: load the most recent manual save, if any.</summary>
    public bool ContinueMostRecent()
    {
        foreach (CitySlot slot in Library.Refresh())
        {
            if (!slot.IsAutosave)
            {
                LoadSlot(slot);
                return true;
            }
        }

        return false;
    }

    /// <summary>Advance the active city by real elapsed time and let autosave run.</summary>
    public void Tick(double realDeltaSeconds)
    {
        if (Active is null)
        {
            return;
        }

        Active.Update(realDeltaSeconds);
        Autosave.Update(Active, TimeSpan.FromSeconds(realDeltaSeconds));
    }

    // --- Library actions (called by the frontend after it collects any needed input) ---

    /// <summary>Save the active city, overwriting its slot if it came from one.</summary>
    public CitySlot SaveActive()
    {
        GameSimulation sim = Active ?? throw new InvalidOperationException("No active city to save.");
        CitySlot slot = Library.Save(sim, into: ActiveSlot);
        ActiveSlot = slot;
        return slot;
    }

    public CitySlot RenameSlot(in CitySlot slot, string newCityName)
    {
        CitySlot renamed = Library.Rename(slot, newCityName);
        if (ActiveSlot is { } active && active.FilePath == slot.FilePath)
        {
            ActiveSlot = renamed;
        }

        return renamed;
    }

    public CitySlot DuplicateSlot(in CitySlot slot, string? copyName = null) => Library.Duplicate(slot, copyName);

    public void DeleteSlot(in CitySlot slot) => Library.Delete(slot);

    public void ExportSlot(in CitySlot slot, Stream destination) => CityPackage.Export(slot, destination);

    public ImportResult ImportPackage(Stream source) => CityPackage.Import(source, Library);

    private void SetActive(GameSimulation sim, CitySlot? slot)
    {
        Active = sim;
        ActiveSlot = slot;
        sim.Start();
        ActiveChanged?.Invoke(sim);
    }
}
