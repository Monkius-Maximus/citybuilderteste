using CityBuilder.Ecs;
using CityBuilder.Grid;

namespace CityBuilder.Data;

/// <summary>
/// Factory that instantiates live ECS entities from data definitions. Callers pass a
/// definition id + placement; the factory assembles the entity by composing the right
/// components (position, building/vehicle tag, movement...) based on the definition's data.
/// This is the Factory pattern: content is added as data, not as new subclasses.
/// </summary>
public interface IEntityFactory
{
    Entity CreateBuilding(string definitionId, GridCoord cell);

    Entity CreateVehicle(string definitionId, GridCoord cell);
}
