using CityBuilder.Shell;

namespace CityBuilder.Library;

/// <summary>
/// Executes the autosave policy the player picked on the Settings screen
/// (<see cref="GameSettings.Autosave"/>): the host feeds real elapsed time each frame and,
/// when the interval is due, the current city is written to a rotating slot
/// (<c>&lt;base&gt;.auto1..N.polis</c>, default 5). The oldest slot is overwritten once the
/// rotation is full. Real wall-clock time on purpose — autosave is player comfort, not
/// simulation state, so it must never affect determinism.
/// </summary>
public sealed class AutosaveService
{
    private readonly CityLibrary _library;
    private readonly GameSettings _settings;
    private TimeSpan _sinceLast;

    public AutosaveService(CityLibrary library, GameSettings settings, int rotationSlots = 5)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        RotationSlots = Math.Max(1, rotationSlots);
    }

    public int RotationSlots { get; }

    /// <summary>The slot written by the most recent autosave (null until the first fires).</summary>
    public CitySlot? LastAutosave { get; private set; }

    /// <summary>
    /// Accumulate elapsed real time; when the configured interval is reached, autosave and
    /// reset. Returns true when an autosave fired this call. Interval changes (the player
    /// re-applied Settings) take effect immediately.
    /// </summary>
    public bool Update(GameSimulation sim, TimeSpan realElapsed)
    {
        TimeSpan? interval = IntervalOf(_settings.Autosave);
        if (interval is null)
        {
            _sinceLast = TimeSpan.Zero; // Off: don't accumulate toward a surprise save later
            return false;
        }

        _sinceLast += realElapsed;
        if (_sinceLast < interval.Value)
        {
            return false;
        }

        _sinceLast = TimeSpan.Zero;
        LastAutosave = SaveNow(sim);
        return true;
    }

    /// <summary>Write an autosave immediately (also used by "save on exit" hosts).</summary>
    public CitySlot SaveNow(GameSimulation sim)
    {
        _library.EnsureDirectory();
        string baseName = _library.MintFileName(sim.Config);
        string stem = baseName.Substring(0, baseName.Length - CityLibrary.SaveExtension.Length);

        return _library.Save(sim, fileName: PickSlotFileName(stem));
    }

    /// <summary>Autosave slots for this city's base name, ordered oldest write first.</summary>
    public IReadOnlyList<CitySlot> SlotsFor(GameSimulation sim)
    {
        string baseName = _library.MintFileName(sim.Config);
        string stem = baseName.Substring(0, baseName.Length - CityLibrary.SaveExtension.Length);

        var slots = new List<CitySlot>();
        foreach (CitySlot slot in _library.Refresh())
        {
            if (slot.IsAutosave && slot.FileName.StartsWith(stem + ".auto", StringComparison.Ordinal))
            {
                slots.Add(slot);
            }
        }

        slots.Sort(static (a, b) => a.Metadata.SavedAtUtc.CompareTo(b.Metadata.SavedAtUtc));
        return slots;
    }

    private string PickSlotFileName(string stem)
    {
        // Fill free indices 1..N first; once full, overwrite the oldest-written slot.
        string? oldestPath = null;
        DateTime oldestWrite = DateTime.MaxValue;

        for (int i = 1; i <= RotationSlots; i++)
        {
            string candidate = $"{stem}.auto{i}{CityLibrary.SaveExtension}";
            string fullPath = Path.Combine(_library.Directory, candidate);

            if (!File.Exists(fullPath))
            {
                return candidate;
            }

            DateTime written = File.GetLastWriteTimeUtc(fullPath);
            if (written < oldestWrite)
            {
                oldestWrite = written;
                oldestPath = candidate;
            }
        }

        return oldestPath!;
    }

    private static TimeSpan? IntervalOf(AutosaveInterval setting) => setting switch
    {
        AutosaveInterval.Every5Min => TimeSpan.FromMinutes(5),
        AutosaveInterval.Every10Min => TimeSpan.FromMinutes(10),
        AutosaveInterval.Every30Min => TimeSpan.FromMinutes(30),
        _ => null, // Off
    };
}
