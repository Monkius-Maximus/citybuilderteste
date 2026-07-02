namespace CityBuilder.Ecs.Components;

/// <summary>
/// Tags an entity as an instance of a building definition. Stores the compact definition
/// index (not a string) so the component stays blittable and cache-friendly.
/// </summary>
public struct BuildingComponent : IComponent
{
    public int DefinitionIndex;

    /// <summary>Growth level 0..255, driven by the zoning/CA + economy systems.</summary>
    public byte Level;
}
