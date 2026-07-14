using System.Text;
using CityBuilder.Grid;

namespace CityBuilder.Library;

/// <summary>
/// A shareable text code that recreates a city's FOUNDING setup — map size, terrain preset and
/// seed — but not the built world (the deterministic <see cref="TerrainGenerator"/> rebuilds the
/// map identically from these three). The city name is intentionally left out (players name
/// their own copy). Two formats, both with an integrity check so a mistyped code is rejected
/// with a clear message rather than silently making the wrong world:
/// <list type="bullet">
///   <item><b>Readable</b>: <c>POLIS-T128-VP-314159-7K</c> — easy to read out or paste in chat.</item>
///   <item><b>Compact</b>: Crockford base32 of the packed fields — case-insensitive, short,
///   loss-tolerant (I/L→1, O→0).</item>
/// </list>
/// </summary>
public static class FoundingCode
{
    private const string Prefix = "POLIS";
    private const byte Version = 1;

    // Crockford base32 (no I, L, O, U): unambiguous to type.
    private const string Base32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string Encode(in GameConfig config, bool compact = false)
        => compact ? EncodeCompact(config) : EncodeReadable(config);

    /// <summary>Human-friendly form. Assumes a square map (what New City produces).</summary>
    public static string EncodeReadable(in GameConfig config)
    {
        string check = ToBase36(CheckValue(config) % 1296, 2);
        return $"{Prefix}-T{config.Width}-{TerrainToken(config.Terrain)}-{config.Seed}-{check}";
    }

    /// <summary>Compact base32 form; round-trips any width/height/seed/terrain.</summary>
    public static string EncodeCompact(in GameConfig config)
    {
        Span<byte> payload = stackalloc byte[18];
        payload[0] = Version;
        payload[1] = (byte)config.Terrain;
        WriteInt32(payload.Slice(2), config.Width);
        WriteInt32(payload.Slice(6), config.Height);
        WriteUInt64(payload.Slice(10), config.Seed);

        Span<byte> withCheck = stackalloc byte[19];
        payload.CopyTo(withCheck);
        withCheck[18] = Fnv8(payload);

        return ToBase32(withCheck);
    }

    /// <summary>Auto-detects the format. On failure, <paramref name="error"/> explains why.</summary>
    public static bool TryDecode(string? code, out GameConfig config, out string? error)
    {
        config = default;
        error = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "Empty founding code.";
            return false;
        }

        string trimmed = code!.Trim();
        return trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            ? TryDecodeReadable(trimmed, out config, out error)
            : TryDecodeCompact(trimmed, out config, out error);
    }

    private static bool TryDecodeReadable(string code, out GameConfig config, out string? error)
    {
        config = default;
        error = null;

        string[] parts = code.Split('-');
        if (parts.Length != 5)
        {
            error = "Malformed code (expected POLIS-T<size>-<terrain>-<seed>-<check>).";
            return false;
        }

        if (parts[1].Length < 2 || (parts[1][0] != 'T' && parts[1][0] != 't')
            || !int.TryParse(parts[1].Substring(1), out int size) || size <= 0)
        {
            error = "Invalid map size token.";
            return false;
        }

        if (!TryTerrainFromToken(parts[2], out TerrainPreset terrain))
        {
            error = $"Unknown terrain '{parts[2]}'.";
            return false;
        }

        if (!ulong.TryParse(parts[3], out ulong seed))
        {
            error = "Invalid seed.";
            return false;
        }

        var candidate = new GameConfig(size, size, seed, terrain: terrain);
        if (!string.Equals(parts[4], ToBase36(CheckValue(candidate) % 1296, 2), StringComparison.OrdinalIgnoreCase))
        {
            error = "Check digits don't match — the code was mistyped.";
            return false;
        }

        config = candidate;
        return true;
    }

    private static bool TryDecodeCompact(string code, out GameConfig config, out string? error)
    {
        config = default;
        error = null;

        if (!TryFromBase32(code, out byte[] bytes) || bytes.Length != 19)
        {
            error = "Not a valid compact founding code.";
            return false;
        }

        var payload = new ReadOnlySpan<byte>(bytes, 0, 18);
        if (bytes[18] != Fnv8(payload))
        {
            error = "Checksum failed — the code is corrupt or mistyped.";
            return false;
        }

        if (bytes[0] != Version)
        {
            error = $"Unsupported founding-code version {bytes[0]}.";
            return false;
        }

        var terrain = (TerrainPreset)bytes[1];
        int width = ReadInt32(payload.Slice(2));
        int height = ReadInt32(payload.Slice(6));
        ulong seed = ReadUInt64(payload.Slice(10));

        if (width <= 0 || height <= 0)
        {
            error = "Invalid map dimensions in code.";
            return false;
        }

        config = new GameConfig(width, height, seed, terrain: terrain);
        return true;
    }

    // --- Terrain tokens ---

    private static string TerrainToken(TerrainPreset t) => t switch
    {
        TerrainPreset.VerdantPlains => "VP",
        TerrainPreset.RiverDelta => "RD",
        TerrainPreset.CoastalReach => "CR",
        TerrainPreset.Highlands => "HL",
        _ => "VP",
    };

    private static bool TryTerrainFromToken(string token, out TerrainPreset terrain)
    {
        switch (token.ToUpperInvariant())
        {
            case "VP": terrain = TerrainPreset.VerdantPlains; return true;
            case "RD": terrain = TerrainPreset.RiverDelta; return true;
            case "CR": terrain = TerrainPreset.CoastalReach; return true;
            case "HL": terrain = TerrainPreset.Highlands; return true;
            default: terrain = TerrainPreset.VerdantPlains; return false;
        }
    }

    // --- Integrity ---

    private static ulong CheckValue(in GameConfig config)
    {
        ulong h = 1469598103934665603UL;
        h = (h ^ (byte)config.Terrain) * 1099511628211UL;
        h = (h ^ (uint)config.Width) * 1099511628211UL;
        h = (h ^ (uint)config.Height) * 1099511628211UL;
        h = (h ^ config.Seed) * 1099511628211UL;
        return h;
    }

    private static byte Fnv8(ReadOnlySpan<byte> data)
    {
        ulong h = 1469598103934665603UL;
        for (int i = 0; i < data.Length; i++)
        {
            h = (h ^ data[i]) * 1099511628211UL;
        }

        return (byte)(h & 0xFF);
    }

    // --- Encodings ---

    private static string ToBase36(ulong value, int length)
    {
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<char> buffer = stackalloc char[length];
        for (int i = length - 1; i >= 0; i--)
        {
            buffer[i] = digits[(int)(value % 36)];
            value /= 36;
        }

        return buffer.ToString();
    }

    private static string ToBase32(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        for (int i = 0; i < data.Length; i++)
        {
            buffer = (buffer << 8) | data[i];
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(Base32[(buffer >> bits) & 0x1F]);
            }
        }

        if (bits > 0)
        {
            sb.Append(Base32[(buffer << (5 - bits)) & 0x1F]);
        }

        return sb.ToString();
    }

    private static bool TryFromBase32(string code, out byte[] bytes)
    {
        var values = new List<int>(code.Length);
        foreach (char raw in code)
        {
            char c = char.ToUpperInvariant(raw);
            c = c switch { 'I' or 'L' => '1', 'O' => '0', _ => c };
            int index = Base32.IndexOf(c);
            if (index < 0)
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            values.Add(index);
        }

        var output = new List<byte>(values.Count * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (int v in values)
        {
            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((buffer >> bits) & 0xFF));
            }
        }

        bytes = output.ToArray();
        return true;
    }

    private static void WriteInt32(Span<byte> dst, int value)
    {
        dst[0] = (byte)value;
        dst[1] = (byte)(value >> 8);
        dst[2] = (byte)(value >> 16);
        dst[3] = (byte)(value >> 24);
    }

    private static int ReadInt32(ReadOnlySpan<byte> src)
        => src[0] | (src[1] << 8) | (src[2] << 16) | (src[3] << 24);

    private static void WriteUInt64(Span<byte> dst, ulong value)
    {
        for (int i = 0; i < 8; i++)
        {
            dst[i] = (byte)(value >> (i * 8));
        }
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> src)
    {
        ulong value = 0;
        for (int i = 0; i < 8; i++)
        {
            value |= (ulong)src[i] << (i * 8);
        }

        return value;
    }
}
