using CityBuilder.Library;

namespace CityBuilder.Shell;

/// <summary>
/// Thin façade kept for the Load City screen: scanning now lives in
/// <see cref="CityLibrary"/> (the full CRUD manager); this just exposes the read path with the
/// same one-call shape it always had. Rows come back manual-saves-first, most recent first.
/// </summary>
public static class SaveCatalog
{
    /// <summary>Save-file extension for THE GAME OF POLIS.</summary>
    public const string Extension = CityLibrary.SaveExtension;

    public static IReadOnlyList<CitySlot> Scan(string directory)
        => new CityLibrary(directory).Refresh();
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
