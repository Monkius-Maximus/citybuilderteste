namespace CityBuilder.Grid;

/// <summary>The terrain options offered on the New City screen. Names match the approved copy.</summary>
public enum TerrainPreset : byte
{
    VerdantPlains = 0,
    RiverDelta = 1,
    CoastalReach = 2,
    Highlands = 3,
}

/// <summary>
/// Deterministic procedural terrain: hashed value-noise (integer mixing + bilinear smoothing)
/// shaped by a <see cref="TerrainPreset"/>. No transcendental functions, so the same seed
/// produces bit-identical terrain on every platform — a save's config alone regenerates the
/// preview, and New City is reproducible. Runs once at city founding (loads restore terrain
/// from the snapshot instead).
/// </summary>
public static class TerrainGenerator
{
    public static void Generate(GridLayer<TerrainCell> terrain, ulong seed, TerrainPreset preset)
    {
        int w = terrain.Width;
        int h = terrain.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Two independent low-frequency fields: one shapes elevation, one vegetation.
                float elevationNoise = Fbm(seed, x, y, baseCell: 16, octaves: 3);
                float vegetationNoise = Fbm(seed ^ 0x9E3779B97F4A7C15UL, x, y, baseCell: 10, octaves: 2);

                terrain[x, y] = Classify(preset, x, y, w, h, elevationNoise, vegetationNoise, seed);
            }
        }
    }

    private static TerrainCell Classify(
        TerrainPreset preset, int x, int y, int w, int h, float elevation, float vegetation, ulong seed)
    {
        var cell = new TerrainCell();

        switch (preset)
        {
            case TerrainPreset.RiverDelta:
            {
                // A river meanders vertically: its centreline is offset per-row by 1D noise.
                float offset = (Value1D(seed, y, 12) - 0.5f) * 0.35f;
                float center = w * (0.5f + offset);
                float halfWidth = w * 0.05f + Value1D(seed ^ 0xABCDUL, y, 9) * w * 0.03f;
                float distance = Math.Abs(x - center);

                if (distance < halfWidth)
                {
                    cell.Kind = TerrainKind.Water;
                    return cell;
                }

                if (distance < halfWidth + 2f)
                {
                    cell.Kind = TerrainKind.Sand;
                    return cell;
                }

                cell.Kind = vegetation > 0.70f ? TerrainKind.Forest : TerrainKind.Grass;
                cell.Elevation = (short)(elevation * 3f);
                return cell;
            }

            case TerrainPreset.CoastalReach:
            {
                // Sea along the +Y edge; the shoreline wobbles with 1D noise.
                float shore = h * (0.72f + (Value1D(seed, x, 14) - 0.5f) * 0.16f);
                if (y > shore)
                {
                    cell.Kind = TerrainKind.Water;
                    return cell;
                }

                if (y > shore - 3f)
                {
                    cell.Kind = TerrainKind.Sand;
                    return cell;
                }

                cell.Kind = vegetation > 0.74f ? TerrainKind.Forest : TerrainKind.Grass;
                cell.Elevation = (short)(elevation * 3f);
                return cell;
            }

            case TerrainPreset.Highlands:
            {
                float scaled = elevation * 8f;
                cell.Elevation = (short)scaled;

                if (scaled > 5.5f)
                {
                    cell.Kind = TerrainKind.Rock;
                }
                else if (elevation < 0.16f)
                {
                    cell.Kind = TerrainKind.Water; // mountain lakes in the lowest basins
                }
                else
                {
                    cell.Kind = vegetation > 0.62f ? TerrainKind.Forest : TerrainKind.Grass;
                }

                return cell;
            }

            default: // VerdantPlains
            {
                cell.Elevation = (short)(elevation * 3f);

                if (elevation < 0.14f)
                {
                    cell.Kind = TerrainKind.Water; // occasional ponds
                }
                else
                {
                    cell.Kind = vegetation > 0.72f ? TerrainKind.Forest : TerrainKind.Grass;
                }

                return cell;
            }
        }
    }

    // --- Hashed value noise (all-integer mixing; bilinear float smoothing only) ---

    /// <summary>Fractal (fBm-style) sum of value-noise octaves, normalised to [0,1).</summary>
    private static float Fbm(ulong seed, int x, int y, int baseCell, int octaves)
    {
        float sum = 0f;
        float amplitude = 1f;
        float total = 0f;
        int cellSize = baseCell;

        for (int o = 0; o < octaves; o++)
        {
            sum += Value2D(seed + (ulong)o * 0x51_7C_C1B7UL, x, y, cellSize) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            cellSize = Math.Max(2, cellSize / 2);
        }

        return sum / total;
    }

    /// <summary>Bilinear value noise: hash lattice corners, smooth-interpolate inside the cell.</summary>
    private static float Value2D(ulong seed, int x, int y, int cellSize)
    {
        int cx = FloorDiv(x, cellSize);
        int cy = FloorDiv(y, cellSize);
        float fx = Smooth((x - cx * cellSize) / (float)cellSize);
        float fy = Smooth((y - cy * cellSize) / (float)cellSize);

        float c00 = Hash01(seed, cx, cy);
        float c10 = Hash01(seed, cx + 1, cy);
        float c01 = Hash01(seed, cx, cy + 1);
        float c11 = Hash01(seed, cx + 1, cy + 1);

        float top = c00 + (c10 - c00) * fx;
        float bottom = c01 + (c11 - c01) * fx;
        return top + (bottom - top) * fy;
    }

    /// <summary>1D variant used for shorelines and river centrelines.</summary>
    private static float Value1D(ulong seed, int t, int cellSize)
    {
        int c = FloorDiv(t, cellSize);
        float f = Smooth((t - c * cellSize) / (float)cellSize);
        float a = Hash01(seed, c, unchecked((int)0x5DEECE66));
        float b = Hash01(seed, c + 1, unchecked((int)0x5DEECE66));
        return a + (b - a) * f;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t); // smoothstep

    /// <summary>Integer coordinate hash → [0,1). SplitMix64-style mixing; fully deterministic.</summary>
    private static float Hash01(ulong seed, int x, int y)
    {
        ulong z = seed ^ ((ulong)(uint)x * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL);
        z ^= z >> 30;
        z *= 0xBF58476D1CE4E5B9UL;
        z ^= z >> 27;
        z *= 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 40) * (1.0f / 16777216.0f);
    }

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        return a < 0 && a != q * b ? q - 1 : q;
    }
}
