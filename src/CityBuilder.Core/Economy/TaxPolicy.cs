using CityBuilder.Zoning;

namespace CityBuilder.Economy;

/// <summary>
/// Player-tunable tax rates per zone category. Rates are clamped to [0,1]. Player commands
/// (e.g. SetTaxRateCommand) mutate this through the <see cref="ITaxPolicy"/> contract.
/// </summary>
public sealed class TaxPolicy : ITaxPolicy
{
    private readonly Dictionary<ZoneType, float> _rates = new()
    {
        [ZoneType.Residential] = 0.09f,
        [ZoneType.Commercial] = 0.09f,
        [ZoneType.Industrial] = 0.09f,
    };

    public float GetRate(ZoneType category) => _rates.TryGetValue(category, out float r) ? r : 0f;

    public void SetRate(ZoneType category, float rate)
    {
        if (rate < 0f)
        {
            rate = 0f;
        }
        else if (rate > 1f)
        {
            rate = 1f;
        }

        _rates[category] = rate;
    }
}
