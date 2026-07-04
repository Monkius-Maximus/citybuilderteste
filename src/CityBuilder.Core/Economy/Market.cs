namespace CityBuilder.Economy;

/// <summary>
/// A single-resource market. Agents submit supply and demand over a period; <see cref="Clear"/>
/// derives a price by nudging the base price along the demand/supply ratio (clamped so prices
/// stay sane). Deterministic. Call <see cref="Reset"/> after reading the cleared result.
/// </summary>
public sealed class Market : IMarket
{
    private readonly Money _basePrice;
    private float _supply;
    private float _demand;
    private Money _price;

    public Market(ResourceKind resource, Money basePrice)
    {
        Resource = resource;
        _basePrice = basePrice;
        _price = basePrice;
    }

    public ResourceKind Resource { get; }
    public Money ClearingPrice => _price;
    public float Supply => _supply;
    public float Demand => _demand;

    public void SubmitSupply(float quantity) => _supply += quantity;
    public void SubmitDemand(float quantity) => _demand += quantity;

    /// <summary>Compute the clearing price from the accumulated supply/demand.</summary>
    public void Clear()
    {
        float ratio;
        if (_supply > 0.001f)
        {
            ratio = _demand / _supply;
        }
        else
        {
            ratio = _demand > 0.001f ? 2f : 1f; // demand with no supply => price ceiling
        }

        if (ratio < 0.5f)
        {
            ratio = 0.5f;
        }
        else if (ratio > 2f)
        {
            ratio = 2f;
        }

        _price = new Money((long)(_basePrice.Units * ratio));
    }

    /// <summary>Clear the accumulated supply/demand for the next period (price is retained).</summary>
    public void Reset()
    {
        _supply = 0f;
        _demand = 0f;
    }
}
