namespace CityBuilder.Economy;

/// <summary>
/// Tunable economic constants (money is integer minor units for determinism). Designers
/// adjust these without touching the economy logic. All "per development-unit" values are the
/// yield at a 100% tax rate; the actual rate scales them.
/// </summary>
public readonly struct EconomySettings
{
    /// <summary>Tax yield per residential development-unit at full rate.</summary>
    public readonly Money ResidentialTaxBase;
    public readonly Money CommercialTaxBase;
    public readonly Money IndustrialTaxBase;

    /// <summary>Upkeep charged per directed road edge each period.</summary>
    public readonly Money RoadUpkeepPerEdge;

    /// <summary>Revenue billed per unit of served utility demand.</summary>
    public readonly Money UtilityRevenuePerDemand;

    /// <summary>Operating cost per unit of installed utility supply capacity.</summary>
    public readonly Money UtilityUpkeepPerCapacity;

    /// <summary>Reference prices used as the neutral point for market clearing.</summary>
    public readonly Money LabourBasePrice;
    public readonly Money GoodsBasePrice;

    public EconomySettings(
        Money residentialTaxBase,
        Money commercialTaxBase,
        Money industrialTaxBase,
        Money roadUpkeepPerEdge,
        Money utilityRevenuePerDemand,
        Money utilityUpkeepPerCapacity,
        Money labourBasePrice,
        Money goodsBasePrice)
    {
        ResidentialTaxBase = residentialTaxBase;
        CommercialTaxBase = commercialTaxBase;
        IndustrialTaxBase = industrialTaxBase;
        RoadUpkeepPerEdge = roadUpkeepPerEdge;
        UtilityRevenuePerDemand = utilityRevenuePerDemand;
        UtilityUpkeepPerCapacity = utilityUpkeepPerCapacity;
        LabourBasePrice = labourBasePrice;
        GoodsBasePrice = goodsBasePrice;
    }

    public static EconomySettings Default => new(
        residentialTaxBase: new Money(100),
        commercialTaxBase: new Money(150),
        industrialTaxBase: new Money(120),
        roadUpkeepPerEdge: new Money(5),
        utilityRevenuePerDemand: new Money(20),
        utilityUpkeepPerCapacity: new Money(8),
        labourBasePrice: Money.FromWhole(10),
        goodsBasePrice: Money.FromWhole(12));
}
