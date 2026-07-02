namespace CityBuilder.Common;

/// <summary>
/// Optional contract for objects managed by an <see cref="ObjectPool{T}"/>.
/// Implement it to reset transient state on rent/return without allocating.
/// </summary>
public interface IPoolable
{
    /// <summary>Called right after the instance leaves the pool.</summary>
    void OnRent();

    /// <summary>Called right before the instance re-enters the pool. Clear references here.</summary>
    void OnReturn();
}
