using CityBuilder.Zoning;

namespace CityBuilder.Population;

/// <summary>
/// The Residential/Commercial/Industrial (RCI) demand model — the beating heart of a city
/// builder. From the developed zoning it derives population, workforce and jobs, then computes
/// each category's demand in [-1,1]: positive pulls that zone type to grow, negative makes it
/// shrink. The feedback loop is the game:
/// <list type="bullet">
///   <item>Residential demand rises with unfilled jobs and desirability (people move to work).</item>
///   <item>Commercial demand rises with unserved population (shops chase customers).</item>
///   <item>Industrial demand rises with export baseline and spare workers (factories chase labour).</item>
/// </list>
/// Taxes suppress each category's demand, so the tax command steers growth, not just revenue.
/// Fully deterministic: derived quantities are integers, ratios evaluated in a fixed order.
/// </summary>
public sealed class DemandModel
{
    private readonly DemandSettings _settings;

    public DemandModel(DemandSettings settings) => _settings = settings;

    public DemandSettings Settings => _settings;

    // Derived city state (from the last Update).
    public long Population { get; private set; }
    public long Workers { get; private set; }
    public long CommercialJobs { get; private set; }
    public long IndustrialJobs { get; private set; }
    public long Jobs => CommercialJobs + IndustrialJobs;
    public long Employed { get; private set; }
    public float EmploymentRate { get; private set; }

    // RCI demand indices in [-1,1].
    public float Residential { get; private set; }
    public float Commercial { get; private set; }
    public float Industrial { get; private set; }

    public float DemandFor(ZoneType type) => type switch
    {
        ZoneType.Residential => Residential,
        ZoneType.Commercial => Commercial,
        ZoneType.Industrial => Industrial,
        _ => 0f,
    };

    /// <summary>
    /// Recompute demand from the current development sums, average desirability over zoned land
    /// and the per-category tax rates.
    /// </summary>
    public void Update(
        long residentialDev,
        long commercialDev,
        long industrialDev,
        float averageDesirability,
        float residentialTax,
        float commercialTax,
        float industrialTax)
    {
        Population = (long)(residentialDev * _settings.ResidentsPerDevelopment);
        Workers = (long)(Population * _settings.WorkforceFraction);
        CommercialJobs = (long)(commercialDev * _settings.JobsPerDevelopment);
        IndustrialJobs = (long)(industrialDev * _settings.JobsPerDevelopment);

        Employed = Math.Min(Workers, Jobs);
        EmploymentRate = Workers > 0 ? Employed / (float)Workers : 0f;

        float scale = _settings.DemandScale;
        float penalty = _settings.TaxDemandPenalty;

        long unfilledJobs = Math.Max(0, Jobs - Workers);
        long spareWorkers = Math.Max(0, Workers - Jobs);
        float desirabilityBias = (averageDesirability - 0.4f) * 0.6f;

        Residential = Clamp(unfilledJobs / scale + desirabilityBias - residentialTax * penalty);

        float unserved = Population - commercialDev * _settings.ShopCapacityPerDevelopment;
        Commercial = Clamp(unserved / scale - commercialTax * penalty);

        Industrial = Clamp(_settings.BaselineIndustrialExport + spareWorkers / scale - industrialTax * penalty);
    }

    private static float Clamp(float value) => value < -1f ? -1f : (value > 1f ? 1f : value);
}
