namespace CityBuilder.Presentation;

/// <summary>
/// Engine-neutral 8-bit RGBA colour. Lives in Core so procedural placeholder visuals can be
/// described without referencing UnityEngine.Color / Godot.Color. The presentation layer maps
/// it to the engine's own colour type at draw time.
/// </summary>
public readonly struct Color32
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public Color32(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static readonly Color32 White = new(255, 255, 255);
    public static readonly Color32 Black = new(0, 0, 0);

    /// <summary>Linear interpolation between two colours; <paramref name="t"/> is clamped to [0,1].</summary>
    public static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        t = t < 0f ? 0f : (t > 1f ? 1f : t);
        return new Color32(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}
