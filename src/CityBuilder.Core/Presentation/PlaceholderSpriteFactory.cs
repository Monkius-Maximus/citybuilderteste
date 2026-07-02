using CityBuilder.Data;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Zoning;

namespace CityBuilder.Presentation;

/// <summary>
/// Default procedural visuals: fixed colour palette + development-driven extrusion. Fully
/// deterministic (no art dependencies). Residential = green, Commercial = blue,
/// Industrial = yellow; a building's prism grows taller with its development level.
/// </summary>
public sealed class PlaceholderSpriteFactory : IProceduralSpriteFactory
{
    private static readonly Color32 Outline = new(20, 20, 28);

    public TileVisual Terrain(TerrainKind kind, short elevation)
    {
        Color32 fill = kind switch
        {
            TerrainKind.Water => new Color32(48, 96, 200),
            TerrainKind.Sand => new Color32(214, 198, 140),
            TerrainKind.Rock => new Color32(120, 120, 128),
            TerrainKind.Forest => new Color32(30, 110, 60),
            _ => new Color32(96, 168, 88), // Grass
        };

        return new TileVisual(IsoShape.FlatDiamond, fill, Outline);
    }

    public TileVisual Zone(ZoneType type, ZoneDensity density, byte development)
    {
        Color32 baseColor = type switch
        {
            ZoneType.Residential => new Color32(64, 180, 96),
            ZoneType.Commercial => new Color32(72, 128, 220),
            ZoneType.Industrial => new Color32(220, 196, 72),
            ZoneType.Civic => new Color32(180, 90, 200),
            _ => new Color32(140, 140, 140),
        };

        if (development == 0)
        {
            // Undeveloped zoning: flat, semi-transparent tint on the ground.
            return new TileVisual(IsoShape.FlatDiamond, WithAlpha(baseColor, 120), Outline);
        }

        // Developed: extrude a prism whose height tracks growth (bounded to keep it readable).
        byte height = (byte)Math.Min(255, 1 + development / 6);
        Color32 lit = Color32.Lerp(baseColor, Color32.White, development / 512f);
        return new TileVisual(IsoShape.Prism, lit, Outline, height);
    }

    public TileVisual Vehicle(VehicleClass vehicleClass)
    {
        Color32 fill = vehicleClass switch
        {
            VehicleClass.Freight => new Color32(200, 120, 40),
            VehicleClass.Transit => new Color32(220, 60, 60),
            VehicleClass.Service => new Color32(240, 220, 60),
            _ => new Color32(230, 230, 230), // Passenger
        };

        return new TileVisual(IsoShape.Marker, fill, Outline);
    }

    public TileVisual Network(NetworkType network)
    {
        Color32 fill = network switch
        {
            NetworkType.Rail => new Color32(90, 90, 96),
            NetworkType.WaterPipe => new Color32(40, 130, 210),
            NetworkType.PowerLine => new Color32(230, 210, 70),
            NetworkType.Sewage => new Color32(110, 80, 50),
            _ => new Color32(60, 60, 66), // Road
        };

        return new TileVisual(IsoShape.FlatDiamond, fill, Outline);
    }

    private static Color32 WithAlpha(Color32 c, byte a) => new(c.R, c.G, c.B, a);
}
