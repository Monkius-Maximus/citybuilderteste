namespace CityBuilder.Ecs.Components;

/// <summary>Tags an entity as an instance of a vehicle definition (agent that follows paths).</summary>
public struct VehicleComponent : IComponent
{
    public int DefinitionIndex;

    /// <summary>Current load carried (passengers/freight), ≤ the definition's capacity.</summary>
    public int Load;
}
