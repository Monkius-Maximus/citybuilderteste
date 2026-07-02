using CityBuilder.Data;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Zoning;

namespace CityBuilder.Presentation;

/// <summary>
/// Maps simulation data to placeholder <see cref="TileVisual"/>s (colored iso primitives),
/// so the game is playable/visualisable before any real art exists. Swapping in real sprites
/// later means providing a different implementation — the simulation is unaffected.
/// </summary>
public interface IProceduralSpriteFactory
{
    TileVisual Terrain(TerrainKind kind, short elevation);

    TileVisual Zone(ZoneType type, ZoneDensity density, byte development);

    TileVisual Vehicle(VehicleClass vehicleClass);

    TileVisual Network(NetworkType network);
}
