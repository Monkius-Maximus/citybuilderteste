using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Presentation;

/// <summary>
/// Renders a small RGBA minimap of a city from its terrain + zoning, using the same placeholder
/// palette the game draws with. Produced at SAVE time and stored in the save's metadata block, so
/// the Load City screen shows real thumbnails without loading whole worlds. Output is raw RGBA
/// (row-major, 4 bytes/pixel) — every engine converts that to its own texture with zero external
/// image dependency. Deterministic (nearest-sample downscale, no RNG).
/// </summary>
public static class ThumbnailRenderer
{
    /// <summary>Default thumbnail size — the proportion of the Load City minimap glyph.</summary>
    public const int DefaultWidth = 64;
    public const int DefaultHeight = 44;

    public static byte[] Render(GameSimulation sim, int width = DefaultWidth, int height = DefaultHeight)
    {
        var sprites = new PlaceholderSpriteFactory();
        var buffer = new byte[width * height * 4];

        GridLayer<TerrainCell> terrain = sim.Map.Terrain;
        GridLayer<ZoneCell> zoning = sim.Map.Zoning;
        int mapWidth = sim.Map.Width;
        int mapHeight = sim.Map.Height;

        for (int py = 0; py < height; py++)
        {
            int cy = py * mapHeight / height;
            for (int px = 0; px < width; px++)
            {
                int cx = px * mapWidth / width;
                var coord = new GridCoord(cx, cy);

                ZoneCell zone = zoning[coord];
                Color32 color;
                if (zone.IsDeveloped)
                {
                    color = sprites.Zone(zone.Type, zone.Density, zone.DevelopmentLevel).Fill;
                }
                else
                {
                    TerrainCell t = terrain[coord];
                    color = sprites.Terrain(t.Kind, t.Elevation).Fill;
                }

                int i = (py * width + px) * 4;
                buffer[i] = color.R;
                buffer[i + 1] = color.G;
                buffer[i + 2] = color.B;
                buffer[i + 3] = 255;
            }
        }

        return buffer;
    }
}
