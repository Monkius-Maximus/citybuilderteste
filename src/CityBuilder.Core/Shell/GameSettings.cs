using System.Text;

namespace CityBuilder.Shell;

/// <summary>Autosave choices on the Settings screen.</summary>
public enum AutosaveInterval : byte
{
    Off = 0,
    Every5Min = 1,
    Every10Min = 2,
    Every30Min = 3,
}

/// <summary>
/// The persisted player configuration behind the Settings screen — fields, defaults and ranges
/// exactly as designed. Supports the screen's BACK/APPLY semantics: <see cref="BeginEdit"/>
/// snapshots the committed values, the UI mutates freely, then <see cref="Apply"/> keeps or
/// <see cref="Discard"/> restores. Binary Save/Load round-trips it to a small settings file.
/// </summary>
public sealed class GameSettings
{
    private const int Version = 1;
    private static readonly byte[] Magic = { (byte)'C', (byte)'B', (byte)'S', (byte)'T' };

    // --- Audio (0–100) ---
    public int MasterVolume { get; set; } = 80;
    public int MusicVolume { get; set; } = 60;
    public int AmbienceVolume { get; set; } = 70;

    // --- Graphics ---
    /// <summary>UI scale percent (75–150).</summary>
    public int UiScale { get; set; } = 100;

    public bool VSync { get; set; } = true;
    public bool BuildingAnimations { get; set; } = true;

    // --- Gameplay ---
    public AutosaveInterval Autosave { get; set; } = AutosaveInterval.Every10Min;
    public bool EdgeScrolling { get; set; } = true;
    public bool TutorialTips { get; set; } = true;

    private GameSettings? _editSnapshot;

    /// <summary>Entering the Settings screen: remember the committed state for BACK.</summary>
    public void BeginEdit() => _editSnapshot = Clone();

    /// <summary>APPLY: keep the current values and drop the snapshot.</summary>
    public void Apply() => _editSnapshot = null;

    /// <summary>BACK: restore the values captured by <see cref="BeginEdit"/>.</summary>
    public void Discard()
    {
        if (_editSnapshot is null)
        {
            return;
        }

        CopyFrom(_editSnapshot);
        _editSnapshot = null;
    }

    public GameSettings Clone()
    {
        var copy = new GameSettings();
        copy.CopyFrom(this);
        return copy;
    }

    private void CopyFrom(GameSettings other)
    {
        MasterVolume = other.MasterVolume;
        MusicVolume = other.MusicVolume;
        AmbienceVolume = other.AmbienceVolume;
        UiScale = other.UiScale;
        VSync = other.VSync;
        BuildingAnimations = other.BuildingAnimations;
        Autosave = other.Autosave;
        EdgeScrolling = other.EdgeScrolling;
        TutorialTips = other.TutorialTips;
    }

    // --- Persistence (tiny hand-rolled binary blob, same conventions as SaveGame) ---

    public void Save(Stream stream)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(Version);
        w.Write(MasterVolume);
        w.Write(MusicVolume);
        w.Write(AmbienceVolume);
        w.Write(UiScale);
        w.Write(VSync);
        w.Write(BuildingAnimations);
        w.Write((byte)Autosave);
        w.Write(EdgeScrolling);
        w.Write(TutorialTips);
    }

    public static GameSettings Load(Stream stream)
    {
        using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        byte[] magic = r.ReadBytes(4);
        if (magic.Length != 4 || magic[0] != Magic[0] || magic[1] != Magic[1] || magic[2] != Magic[2] || magic[3] != Magic[3])
        {
            throw new InvalidDataException("Not a settings file.");
        }

        int version = r.ReadInt32();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported settings version {version}.");
        }

        return new GameSettings
        {
            MasterVolume = r.ReadInt32(),
            MusicVolume = r.ReadInt32(),
            AmbienceVolume = r.ReadInt32(),
            UiScale = r.ReadInt32(),
            VSync = r.ReadBoolean(),
            BuildingAnimations = r.ReadBoolean(),
            Autosave = (AutosaveInterval)r.ReadByte(),
            EdgeScrolling = r.ReadBoolean(),
            TutorialTips = r.ReadBoolean(),
        };
    }
}
