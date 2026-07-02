namespace CityBuilder;

/// <summary>Bootstrap parameters for a new simulation. Plain data — no engine dependency.</summary>
public readonly struct GameConfig
{
    public readonly int Width;
    public readonly int Height;
    public readonly ulong Seed;
    public readonly double TicksPerSecond;

    public GameConfig(int width, int height, ulong seed = 1, double ticksPerSecond = 10.0)
    {
        Width = width;
        Height = height;
        Seed = seed;
        TicksPerSecond = ticksPerSecond;
    }

    public static GameConfig Default => new(64, 64);
}
