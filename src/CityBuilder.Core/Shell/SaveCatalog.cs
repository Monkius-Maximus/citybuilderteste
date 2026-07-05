using CityBuilder.Persistence;

namespace CityBuilder.Shell;

/// <summary>A save file surfaced on the Load City screen: where it lives + its display metadata.</summary>
public readonly struct SaveSlot
{
    public readonly string FilePath;
    public readonly SaveMetadata Metadata;

    public SaveSlot(string filePath, SaveMetadata metadata)
    {
        FilePath = filePath;
        Metadata = metadata;
    }
}

/// <summary>
/// Backs the Load City screen: scans a saves directory, reads each file's metadata block (never
/// the world state) and returns rows sorted most-recent first — the exact order the design shows.
/// Unreadable/corrupt files are skipped rather than crashing the menu.
/// </summary>
public static class SaveCatalog
{
    /// <summary>Save-file extension for THE GAME OF POLIS.</summary>
    public const string Extension = ".polis";

    public static List<SaveSlot> Scan(string directory)
    {
        var slots = new List<SaveSlot>();
        if (!Directory.Exists(directory))
        {
            return slots;
        }

        foreach (string path in Directory.GetFiles(directory, "*" + Extension))
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                slots.Add(new SaveSlot(path, SaveGame.ReadMetadata(stream)));
            }
            catch (IOException)
            {
                // Locked/unreadable file: leave it off the list.
            }
            catch (InvalidDataException)
            {
                // Not a valid save (wrong magic/version): skip silently.
            }
        }

        slots.Sort(static (a, b) => b.Metadata.SavedAtUtc.CompareTo(a.Metadata.SavedAtUtc));
        return slots;
    }
}

/// <summary>
/// Human "last played" labels for save rows ("2 h ago", "yesterday", "last week"), matching the
/// design's sample copy. Pure function of two instants — testable headless.
/// </summary>
public static class RelativeTime
{
    public static string Describe(DateTime utcThen, DateTime utcNow)
    {
        TimeSpan span = utcNow - utcThen;
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        if (span.TotalMinutes < 1)
        {
            return "just now";
        }

        if (span.TotalHours < 1)
        {
            return $"{(int)span.TotalMinutes} min ago";
        }

        if (span.TotalHours < 24)
        {
            return $"{(int)span.TotalHours} h ago";
        }

        if (span.TotalDays < 2)
        {
            return "yesterday";
        }

        if (span.TotalDays < 7)
        {
            return $"{(int)span.TotalDays} days ago";
        }

        if (span.TotalDays < 14)
        {
            return "last week";
        }

        if (span.TotalDays < 60)
        {
            return $"{(int)(span.TotalDays / 7)} weeks ago";
        }

        return utcThen.ToString("yyyy-MM-dd");
    }
}
