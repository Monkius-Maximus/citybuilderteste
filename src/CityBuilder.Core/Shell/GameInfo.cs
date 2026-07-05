namespace CityBuilder.Shell;

/// <summary>
/// Branding and shared copy for every frontend, exactly as approved in the design handoff.
/// Keeping the strings here means the console host, Unity and Godot all present the same game.
/// </summary>
public static class GameInfo
{
    public const string Kicker = "THE GAME OF";
    public const string Title = "POLIS";
    public const string Tagline = "an isometric city builder";

    public const string Version = "PRE-ALPHA 0.1";

    /// <summary>The in-game currency glyph (plain text, no bitmap asset).</summary>
    public const string CurrencyGlyph = "§";

    /// <summary>Title-screen footer line, e.g. "PRE-ALPHA 0.1 · DETERMINISTIC SIMULATION CORE · 10 TICKS / S".</summary>
    public static string FooterLine(double ticksPerSecond)
        => $"{Version} · DETERMINISTIC SIMULATION CORE · {ticksPerSecond:0} TICKS / S";
}
