using CityBuilder.Common;

namespace CityBuilder.App;

/// <summary>
/// Placeholder high-churn actor used to exercise <see cref="ObjectPool{T}"/> in the demo
/// (in the real game this is a vehicle/pedestrian/particle view object). Implements
/// <see cref="IPoolable"/> so its transient state is reset on rent/return with no allocation.
/// </summary>
public sealed class Particle : IPoolable
{
    public float X;
    public float Y;
    public bool Active;

    public void OnRent() => Active = true;

    public void OnReturn()
    {
        Active = false;
        X = 0f;
        Y = 0f;
    }
}
