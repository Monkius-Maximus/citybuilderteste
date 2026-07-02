using CityBuilder.Ecs;
using CityBuilder.Ecs.Components;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;

namespace CityBuilder.Data;

/// <summary>
/// Default factory: reads a definition from the registry and composes an entity from
/// components. Building/vehicle entities are assembled purely by data — adding a new kind of
/// content means adding a definition, not a class. Vehicle entity ids are recycled by the
/// <see cref="EntityRegistry"/>, which is the ECS-level equivalent of object pooling.
/// </summary>
public sealed class EntityFactory : IEntityFactory
{
    private readonly EcsWorld _world;
    private readonly DefinitionRegistry _definitions;
    private readonly IEventBus _events;

    public EntityFactory(EcsWorld world, DefinitionRegistry definitions, IEventBus events)
    {
        _world = world;
        _definitions = definitions;
        _events = events;
    }

    public Entity CreateBuilding(string definitionId, GridCoord cell)
    {
        int index = _definitions.IndexOf(definitionId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Unknown building definition '{definitionId}'.");
        }

        Entity entity = _world.Create();
        _world.Add(entity, new GridPositionComponent(cell));
        _world.Add(entity, new BuildingComponent { DefinitionIndex = index, Level = 1 });

        _events.Publish(new BuildingConstructedEvent(entity, definitionId, cell));
        return entity;
    }

    public Entity CreateVehicle(string definitionId, GridCoord cell)
    {
        int index = _definitions.IndexOf(definitionId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Unknown vehicle definition '{definitionId}'.");
        }

        var definition = (VehicleDefinition)_definitions.GetByIndex(index);

        Entity entity = _world.Create();
        _world.Add(entity, new GridPositionComponent(cell));
        _world.Add(entity, new VehicleComponent { DefinitionIndex = index, Load = 0 });
        // Starts idle (Active = false); the traffic layer assigns a route + current edge.
        _world.Add(entity, new MovementComponent { Speed = definition.MaxSpeed, Active = false });

        return entity;
    }
}
