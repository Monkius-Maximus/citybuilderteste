using CityBuilder.Persistence;

namespace CityBuilder.Library;

/// <summary>
/// One entry in the player's city library: where the save lives on disk plus its cheap display
/// metadata. Replaces the older Shell-level SaveSlot as the canonical slot type (the Load City
/// screen, autosave badges and future import/export all speak this).
/// </summary>
public readonly struct CitySlot
{
    public readonly string FilePath;
    public readonly SaveMetadata Metadata;

    /// <summary>True for rotation slots written by the <see cref="AutosaveService"/> (UI badges these).</summary>
    public readonly bool IsAutosave;

    public CitySlot(string filePath, SaveMetadata metadata, bool isAutosave = false)
    {
        FilePath = filePath;
        Metadata = metadata;
        IsAutosave = isAutosave;
    }

    public string FileName => Path.GetFileName(FilePath);

    public string CityName => Metadata.CityName;
}
