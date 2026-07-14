using System.Globalization;
using CityBuilder.Economy;
using CityBuilder.Simulation;

namespace CityBuilder.Persistence;

/// <summary>
/// The cheap, display-only header of a save — everything the Load City row shows (name,
/// "Population 12,480 · § 45,120 · Year 4 — Spring", relative timestamp) without loading the
/// world. Produced by <see cref="SaveGame.ReadMetadata"/>.
/// </summary>
public readonly struct SaveMetadata
{
    public readonly GameConfig Config;
    public readonly long Population;
    public readonly Money Treasury;
    public readonly long Tick;
    public readonly DateTime SavedAtUtc;

    /// <summary>RGBA minimap (row-major, 4 bytes/pixel); empty for v2 saves that predate thumbnails.</summary>
    public readonly byte[] Thumbnail;
    public readonly int ThumbnailWidth;
    public readonly int ThumbnailHeight;

    public SaveMetadata(
        GameConfig config,
        long population,
        Money treasury,
        long tick,
        DateTime savedAtUtc,
        int thumbnailWidth = 0,
        int thumbnailHeight = 0,
        byte[]? thumbnail = null)
    {
        Config = config;
        Population = population;
        Treasury = treasury;
        Tick = tick;
        SavedAtUtc = savedAtUtc;
        ThumbnailWidth = thumbnailWidth;
        ThumbnailHeight = thumbnailHeight;
        Thumbnail = thumbnail ?? Array.Empty<byte>();
    }

    public bool HasThumbnail => Thumbnail.Length > 0 && ThumbnailWidth > 0 && ThumbnailHeight > 0;

    public string CityName => Config.CityName;

    public int Year => GameCalendar.YearOf(Tick);

    public Season Season => GameCalendar.SeasonOf(Tick);

    /// <summary>The row's metadata line, exactly as designed: "Population 12,480 · § 45,120 · Year 4 — Spring".</summary>
    public string Describe()
        => $"Population {Population.ToString("N0", CultureInfo.InvariantCulture)} · {Treasury} · {GameCalendar.Describe(Tick)}";
}
