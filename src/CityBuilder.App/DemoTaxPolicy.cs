using CityBuilder.Economy;
using CityBuilder.Zoning;

namespace CityBuilder.App;

/// <summary>
/// Minimal in-app stand-in for the (not-yet-built) economy, implementing only the
/// <see cref="ITaxPolicy"/> CONTRACT so we can demonstrate <c>SetTaxRateCommand</c> end to end.
/// It intentionally lives in the app, not in Core — Core ships economy contracts only.
/// </summary>
public sealed class DemoTaxPolicy : ITaxPolicy
{
    private readonly Dictionary<ZoneType, float> _rates = new()
    {
        [ZoneType.Residential] = 0.09f,
        [ZoneType.Commercial] = 0.09f,
        [ZoneType.Industrial] = 0.09f,
    };

    public float GetRate(ZoneType category) => _rates.TryGetValue(category, out float r) ? r : 0f;

    public void SetRate(ZoneType category, float rate) => _rates[category] = rate;
}
