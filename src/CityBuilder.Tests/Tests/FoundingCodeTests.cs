using CityBuilder;
using CityBuilder.Grid;
using CityBuilder.Library;
using CityBuilder.Tests.Framework;

namespace CityBuilder.Tests.Tests;

public static class FoundingCodeTests
{
    private static readonly GameConfig Sample = new(128, 128, 314159, 10, "Nova Polis", TerrainPreset.RiverDelta);

    [TestCase]
    public static void Readable_RoundTrips()
    {
        string code = FoundingCode.EncodeReadable(Sample);
        Check.True(FoundingCode.TryDecode(code, out GameConfig decoded, out _), $"decode readable '{code}'");
        Check.Equal(Sample.Width, decoded.Width, "width");
        Check.Equal(Sample.Seed, decoded.Seed, "seed");
        Check.Equal(Sample.Terrain, decoded.Terrain, "terrain");
    }

    [TestCase]
    public static void Compact_RoundTrips()
    {
        string code = FoundingCode.EncodeCompact(Sample);
        Check.True(FoundingCode.TryDecode(code, out GameConfig decoded, out _), $"decode compact '{code}'");
        Check.Equal(Sample.Width, decoded.Width, "width");
        Check.Equal(Sample.Height, decoded.Height, "height");
        Check.Equal(Sample.Seed, decoded.Seed, "seed");
        Check.Equal(Sample.Terrain, decoded.Terrain, "terrain");
    }

    [TestCase]
    public static void BothFormats_AgreeOnSetup()
    {
        FoundingCode.TryDecode(FoundingCode.EncodeReadable(Sample), out GameConfig r, out _);
        FoundingCode.TryDecode(FoundingCode.EncodeCompact(Sample), out GameConfig c, out _);
        Check.Equal(r.Seed, c.Seed, "seed agrees");
        Check.Equal(r.Terrain, c.Terrain, "terrain agrees");
        Check.Equal(r.Width, c.Width, "width agrees");
    }

    [TestCase]
    public static void MistypedReadable_Rejected()
    {
        // Valid shape, wrong check digits.
        bool ok = FoundingCode.TryDecode("POLIS-T128-VP-314159-ZZ", out _, out string? error);
        // If by fluke ZZ is the real check, this still holds since the true check for VP != RD sample.
        Check.False(ok, $"mistyped code should be rejected (error: {error})");
        Check.True(error is not null, "an error message is returned");
    }

    [TestCase]
    public static void CorruptCompact_Rejected()
    {
        string good = FoundingCode.EncodeCompact(Sample);
        // Flip one character to a different valid base32 symbol.
        char[] chars = good.ToCharArray();
        chars[0] = chars[0] == 'A' ? 'B' : 'A';
        bool ok = FoundingCode.TryDecode(new string(chars), out _, out string? error);
        Check.False(ok, $"corrupt compact code should be rejected (error: {error})");
    }

    [TestCase]
    public static void Empty_Rejected()
    {
        Check.False(FoundingCode.TryDecode("", out _, out _), "empty code rejected");
        Check.False(FoundingCode.TryDecode(null, out _, out _), "null code rejected");
    }
}
