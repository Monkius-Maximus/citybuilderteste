using CityBuilder.Persistence;

namespace CityBuilder.Library;

/// <summary>
/// The player's city manager (TheoTown-style): full CRUD over a directory of <c>.polis</c> saves.
/// <list type="bullet">
///   <item><b>Atomic writes</b> — saves land in a temp file and are swapped in, so a crash
///   mid-save never corrupts the previous good slot.</item>
///   <item><b>Non-destructive delete</b> — slots move to a <c>.trash/</c> subfolder (invisible
///   to <see cref="Refresh"/>), guarding against accidental clicks.</item>
///   <item><b>Stable file names</b> — the file is a slug minted at first save
///   (<c>nova-polis-4c8e.polis</c>); renaming the CITY rewrites metadata in place and never
///   renames the FILE, so references stay valid.</item>
/// </list>
/// Engine-agnostic: the host hands in the root directory; nothing here knows about rendering.
/// </summary>
public sealed class CityLibrary
{
    public const string TrashFolder = ".trash";

    public CityLibrary(string directory)
    {
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    /// <summary>Root folder of this library (created lazily on the first write).</summary>
    public string Directory { get; }

    /// <summary>Raised after any mutation (save/rename/duplicate/delete) so the UI can re-render.</summary>
    public event Action? LibraryChanged;

    /// <summary>
    /// Scan the library: every readable save, manual first then by most recently saved.
    /// Trash content is excluded; unreadable files are skipped, never fatal.
    /// </summary>
    public IReadOnlyList<CitySlot> Refresh()
    {
        var slots = new List<CitySlot>();
        if (!System.IO.Directory.Exists(Directory))
        {
            return slots;
        }

        foreach (string path in System.IO.Directory.GetFiles(Directory, "*" + SaveExtension))
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                SaveMetadata metadata = SaveGame.ReadMetadata(stream);
                slots.Add(new CitySlot(path, metadata, IsAutosaveFile(path)));
            }
            catch (IOException)
            {
                // Locked/unreadable: leave it off the list.
            }
            catch (InvalidDataException)
            {
                // Not a valid save (magic/version): skip silently.
            }
        }

        slots.Sort(static (a, b) =>
        {
            int byKind = a.IsAutosave.CompareTo(b.IsAutosave); // manual saves first
            return byKind != 0 ? byKind : b.Metadata.SavedAtUtc.CompareTo(a.Metadata.SavedAtUtc);
        });

        return slots;
    }

    /// <summary>
    /// Persist a simulation. Pass <paramref name="into"/> to overwrite that exact slot (the
    /// normal "Save" of a loaded city); otherwise an existing slot for the same city (seed,
    /// size and name) is reused, and failing that a fresh slug file is minted.
    /// </summary>
    public CitySlot Save(GameSimulation sim, CitySlot? into = null, string? fileName = null)
    {
        EnsureDirectory();

        string path = into?.FilePath
                      ?? (fileName is not null ? Path.Combine(Directory, fileName) : null)
                      ?? FindExistingPath(sim.Config)
                      ?? Path.Combine(Directory, MintFileName(sim.Config));

        WriteAtomic(path, destination => SaveGame.Write(sim, destination));

        CitySlot slot = ReadSlot(path);
        LibraryChanged?.Invoke();
        return slot;
    }

    /// <summary>
    /// Load a slot into a fresh simulation. <paramref name="bootstrap"/> is the SAME host setup
    /// a new game runs (definitions, scenario systems) — never world content, which comes from
    /// the save (see SaveGame's load-is-rebuild rule).
    /// </summary>
    public GameSimulation Load(in CitySlot slot, Action<GameSimulation> bootstrap)
    {
        using FileStream stream = File.OpenRead(slot.FilePath);
        GameConfig config = SaveGame.ReadConfig(stream);

        var sim = new GameSimulation(config);
        bootstrap(sim);

        stream.Position = 0;
        SaveGame.ReadInto(sim, stream);
        return sim;
    }

    /// <summary>
    /// Rename the CITY (display name) without loading the world and without renaming the file:
    /// the save's name field is rewritten in place, atomically.
    /// </summary>
    public CitySlot Rename(in CitySlot slot, string newCityName)
    {
        if (string.IsNullOrWhiteSpace(newCityName))
        {
            throw new ArgumentException("City name cannot be empty.", nameof(newCityName));
        }

        byte[] bytes = File.ReadAllBytes(slot.FilePath);
        WriteAtomic(slot.FilePath, destination => SaveGame.RewriteCityName(bytes, destination, newCityName));

        CitySlot renamed = ReadSlot(slot.FilePath);
        LibraryChanged?.Invoke();
        return renamed;
    }

    /// <summary>Copy a slot to a new file, giving the copy its own name ("Name (2)" by default).</summary>
    public CitySlot Duplicate(in CitySlot slot, string? copyName = null)
    {
        EnsureDirectory();

        string name = copyName ?? slot.CityName + " (2)";
        string path = UniquePath(Path.Combine(Directory, MintFileName(slot.Metadata.Config, name)));

        byte[] bytes = File.ReadAllBytes(slot.FilePath);
        WriteAtomic(path, destination => SaveGame.RewriteCityName(bytes, destination, name));

        CitySlot copy = ReadSlot(path);
        LibraryChanged?.Invoke();
        return copy;
    }

    /// <summary>
    /// Move a slot to the library's <c>.trash/</c> folder (kept on disk, hidden from the list) —
    /// an accidental delete is recoverable by moving the file back.
    /// </summary>
    public bool Delete(in CitySlot slot)
    {
        if (!File.Exists(slot.FilePath))
        {
            return false;
        }

        string trashDir = Path.Combine(Directory, TrashFolder);
        System.IO.Directory.CreateDirectory(trashDir);

        string target = UniquePath(Path.Combine(trashDir, Path.GetFileName(slot.FilePath)));
        File.Move(slot.FilePath, target);

        LibraryChanged?.Invoke();
        return true;
    }

    /// <summary>Files currently sitting in the trash (for a future "restore/empty trash" UI).</summary>
    public IReadOnlyList<string> TrashContents()
    {
        string trashDir = Path.Combine(Directory, TrashFolder);
        return System.IO.Directory.Exists(trashDir)
            ? System.IO.Directory.GetFiles(trashDir, "*" + SaveExtension)
            : Array.Empty<string>();
    }

    // --- Internals shared with AutosaveService ---

    internal const string SaveExtension = ".polis";

    internal void EnsureDirectory() => System.IO.Directory.CreateDirectory(Directory);

    internal CitySlot ReadSlot(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return new CitySlot(path, SaveGame.ReadMetadata(stream), IsAutosaveFile(path));
    }

    /// <summary>Write via temp file + swap so the previous save survives a mid-write crash.</summary>
    internal void WriteAtomic(string path, Action<Stream> write)
    {
        string temp = path + ".tmp";
        using (FileStream stream = File.Create(temp))
        {
            write(stream);
        }

        if (File.Exists(path))
        {
            File.Replace(temp, path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temp, path);
        }
    }

    /// <summary>"nova-polis-4c8e.polis": slug of the name + short seed hash, unique on collision.</summary>
    internal string MintFileName(in GameConfig config, string? nameOverride = null)
        => $"{Slugify(nameOverride ?? config.CityName)}-{(config.Seed & 0xFFFF):x4}{SaveExtension}";

    /// <summary>Autosaves are "&lt;base&gt;.autoN.polis".</summary>
    internal static bool IsAutosaveFile(string path)
    {
        string name = Path.GetFileName(path);
        if (!name.EndsWith(SaveExtension, StringComparison.Ordinal))
        {
            return false;
        }

        string stem = name.Substring(0, name.Length - SaveExtension.Length);
        int marker = stem.LastIndexOf(".auto", StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        string digits = stem.Substring(marker + 5);
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (char c in digits)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return true;
    }

    private string? FindExistingPath(in GameConfig config)
    {
        foreach (CitySlot slot in Refresh())
        {
            if (slot.IsAutosave)
            {
                continue;
            }

            GameConfig other = slot.Metadata.Config;
            if (other.Seed == config.Seed
                && other.Width == config.Width
                && other.Height == config.Height
                && string.Equals(other.CityName, config.CityName, StringComparison.Ordinal))
            {
                return slot.FilePath;
            }
        }

        return null;
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string dir = Path.GetDirectoryName(path) ?? "";
        string stem = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        for (int i = 2; ; i++)
        {
            string candidate = Path.Combine(dir, $"{stem}-{i}{ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static string Slugify(string name)
    {
        Span<char> buffer = stackalloc char[name.Length];
        int length = 0;
        bool lastDash = true; // suppress leading dashes

        foreach (char c in name)
        {
            char lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                buffer[length++] = lower;
                lastDash = false;
            }
            else if (!lastDash)
            {
                buffer[length++] = '-';
                lastDash = true;
            }
        }

        while (length > 0 && buffer[length - 1] == '-')
        {
            length--;
        }

        return length == 0 ? "city" : buffer.Slice(0, length).ToString();
    }
}
