namespace CityBuilder.Ecs;

/// <summary>
/// Marker for a component: a small, blittable <c>struct</c> of pure data with no
/// behaviour. Components are stored in packed arrays for cache-friendly iteration.
/// Keep them value types — never reference types — to avoid GC pressure and pointer chasing.
/// </summary>
public interface IComponent
{
}
