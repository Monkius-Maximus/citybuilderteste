using CityBuilder.Economy;

namespace CityBuilder.Population;

/// <summary>
/// Tunable constants for the population + RCI demand model. Kept as data so designers balance
/// the growth loop without touching logic; all deterministic (integer money, float ratios
/// evaluated in a fixed order).
/// </summary>
public readonly struct DemandSettings
{
    /// <summary>Residents represented by one unit of residential development.</summary>
    public readonly float ResidentsPerDevelopment;

    /// <summary>Jobs represented by one unit of commercial/industrial development.</summary>
    public readonly float JobsPerDevelopment;

    /// <summary>Share of residents that are in the workforce.</summary>
    public readonly float WorkforceFraction;

    /// <summary>Customers one unit of commercial development can serve.</summary>
    public readonly float ShopCapacityPerDevelopment;

    /// <summary>Normaliser turning "jobs/people gaps" into a [-1,1] demand signal.</summary>
    public readonly float DemandScale;

    /// <summary>Constant outside demand pulling industry upward (exports).</summary>
    public readonly float BaselineIndustrialExport;

    /// <summary>How strongly a category's tax rate suppresses its demand.</summary>
    public readonly float TaxDemandPenalty;

    // --- Sector money circulation (EconomicAgent/Ledger) ---

    /// <summary>Wage paid per employed worker each economic tick.</summary>
    public readonly Money WagePerWorker;

    /// <summary>Fraction of wages households spend at commerce.</summary>
    public readonly float ConsumptionFraction;

    /// <summary>Fraction of commercial sales spent restocking from industry.</summary>
    public readonly float WholesaleFraction;

    public DemandSettings(
        float residentsPerDevelopment,
        float jobsPerDevelopment,
        float workforceFraction,
        float shopCapacityPerDevelopment,
        float demandScale,
        float baselineIndustrialExport,
        float taxDemandPenalty,
        Money wagePerWorker,
        float consumptionFraction,
        float wholesaleFraction)
    {
        ResidentsPerDevelopment = residentsPerDevelopment;
        JobsPerDevelopment = jobsPerDevelopment;
        WorkforceFraction = workforceFraction;
        ShopCapacityPerDevelopment = shopCapacityPerDevelopment;
        DemandScale = demandScale;
        BaselineIndustrialExport = baselineIndustrialExport;
        TaxDemandPenalty = taxDemandPenalty;
        WagePerWorker = wagePerWorker;
        ConsumptionFraction = consumptionFraction;
        WholesaleFraction = wholesaleFraction;
    }

    public static DemandSettings Default => new(
        residentsPerDevelopment: 3f,
        jobsPerDevelopment: 2f,
        workforceFraction: 0.5f,
        shopCapacityPerDevelopment: 6f,
        demandScale: 400f,
        baselineIndustrialExport: 0.25f,
        taxDemandPenalty: 1.5f,
        wagePerWorker: new Money(5),
        consumptionFraction: 0.6f,
        wholesaleFraction: 0.5f);
}
