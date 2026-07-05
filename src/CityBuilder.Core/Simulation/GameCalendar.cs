namespace CityBuilder.Simulation;

public enum Season : byte
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3,
}

/// <summary>
/// Deterministic mapping from the tick counter to game time (Year / Season) — pure integer
/// arithmetic, so every client derives the identical date from the same tick. Used by the
/// Load City screen ("Year 4 — Spring"), seasonal simulation modifiers later, and save metadata.
/// </summary>
public static class GameCalendar
{
    /// <summary>Base ticks per season (~5 min at the default 10 ticks/s).</summary>
    public const long TicksPerSeason = 3_000;

    public const int SeasonsPerYear = 4;

    public const long TicksPerYear = TicksPerSeason * SeasonsPerYear;

    /// <summary>Years are 1-based: a brand-new city is in Year 1.</summary>
    public static int YearOf(long tick) => (int)(tick / TicksPerYear) + 1;

    public static Season SeasonOf(long tick) => (Season)((tick % TicksPerYear) / TicksPerSeason);

    /// <summary>Progress through the current season in [0,1) — drives seasonal tweening in the view.</summary>
    public static float SeasonProgress(long tick) => (tick % TicksPerSeason) / (float)TicksPerSeason;

    /// <summary>Formats game time exactly as the design specifies, e.g. "Year 4 — Spring".</summary>
    public static string Describe(long tick) => $"Year {YearOf(tick)} — {SeasonOf(tick)}";
}
