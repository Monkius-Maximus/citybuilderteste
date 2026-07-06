using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Simulation;
using CityBuilder.Zoning;

namespace CityBuilder.Economy;

/// <summary>
/// The city economic cycle, implemented over the economy contracts. Each economic tick it:
/// taxes developed zones, clears the labour/goods markets from zone supply &amp; demand, bills
/// utility service and charges utility + road upkeep, then settles the treasury and publishes
/// the results. It ties the earlier milestones together — zoning is the tax base and labour
/// pool, utilities bring revenue, infrastructure costs upkeep. Deterministic: integer money,
/// no RNG, sum order-independent.
/// </summary>
public sealed class EconomySystem : IEconomySystem
{
    private const int CityAgentId = EconomicAgentIds.City;

    private readonly WorldMap _map;
    private readonly FlowNetwork _roads;
    private readonly IEventBus _events;
    private readonly EconomySettings _settings;
    private readonly int _interval;

    private readonly Budget _budget;
    private readonly TaxPolicy _taxes;
    private readonly Ledger _ledger;
    private readonly Dictionary<ResourceKind, Market> _markets = new();

    // Latest utility state per network kind — the economy OBSERVES the utility system's events.
    private readonly Dictionary<NetworkType, (float Supply, float Served)> _utility = new();

    public EconomySystem(
        WorldMap map,
        FlowNetwork roads,
        IEventBus events,
        Money startingBalance,
        EconomySettings settings,
        int tickInterval)
    {
        _map = map;
        _roads = roads;
        _events = events;
        _settings = settings;
        _interval = Math.Max(1, tickInterval);

        _budget = new Budget(startingBalance);
        _taxes = new TaxPolicy();
        _ledger = new Ledger();

        _markets[ResourceKind.Labour] = new Market(ResourceKind.Labour, settings.LabourBasePrice);
        _markets[ResourceKind.Goods] = new Market(ResourceKind.Goods, settings.GoodsBasePrice);
        _markets[ResourceKind.Power] = new Market(ResourceKind.Power, Money.FromWhole(5));
        _markets[ResourceKind.Water] = new Market(ResourceKind.Water, Money.FromWhole(4));
        _markets[ResourceKind.Freight] = new Market(ResourceKind.Freight, Money.FromWhole(8));

        events.Subscribe<UtilityUpdatedEvent>(OnUtility);
    }

    // --- IEconomySystem ---
    public IBudget CityBudget => _budget;
    public ITaxPolicy Taxes => _taxes;

    public IMarket GetMarket(ResourceKind resource)
        => _markets.TryGetValue(resource, out Market? m) ? m : throw new KeyNotFoundException($"No market for {resource}.");

    public Ledger Ledger => _ledger;
    public Money Balance => _budget.Balance;

    /// <summary>Overwrite the treasury balance when loading a save (persistence only).</summary>
    public void RestoreBalance(Money balance) => _budget.RestoreBalance(balance);

    // --- ISimulationSystem ---
    public string Name => "Economy";
    public int TickInterval => _interval;

    private void OnUtility(UtilityUpdatedEvent e) => _utility[e.Kind] = (e.Supply, e.ServedDemand);

    public void OnTick(in TickContext context)
    {
        // 1) Tally developed zoning by category (tax base + market supply/demand).
        long devResidential = 0, devCommercial = 0, devIndustrial = 0;
        Span<ZoneCell> cells = _map.Zoning.AsSpan();
        for (int i = 0; i < cells.Length; i++)
        {
            ZoneCell c = cells[i];
            if (c.DevelopmentLevel == 0)
            {
                continue;
            }

            switch (c.Type)
            {
                case ZoneType.Residential: devResidential += c.DevelopmentLevel; break;
                case ZoneType.Commercial: devCommercial += c.DevelopmentLevel; break;
                case ZoneType.Industrial: devIndustrial += c.DevelopmentLevel; break;
            }
        }

        // 2) Taxes.
        Money taxes = TaxYield(_settings.ResidentialTaxBase, devResidential, _taxes.GetRate(ZoneType.Residential))
                    + TaxYield(_settings.CommercialTaxBase, devCommercial, _taxes.GetRate(ZoneType.Commercial))
                    + TaxYield(_settings.IndustrialTaxBase, devIndustrial, _taxes.GetRate(ZoneType.Industrial));
        _budget.RecordIncome(BudgetCategory.Taxes, taxes);

        // 3) Markets: residents supply labour; commerce+industry demand it. Industry makes goods
        //    that commerce sells.
        ClearMarket(ResourceKind.Labour, supply: devResidential, demand: devCommercial + devIndustrial);
        ClearMarket(ResourceKind.Goods, supply: devIndustrial, demand: devCommercial);

        // 4) Utilities: bill served demand, pay upkeep on installed capacity.
        Money utilityRevenue = Money.Zero;
        Money utilityUpkeep = Money.Zero;
        foreach (KeyValuePair<NetworkType, (float Supply, float Served)> kv in _utility)
        {
            utilityRevenue += new Money((long)(_settings.UtilityRevenuePerDemand.Units * kv.Value.Served));
            utilityUpkeep += new Money((long)(_settings.UtilityUpkeepPerCapacity.Units * kv.Value.Supply));
        }

        if (utilityRevenue.Units != 0)
        {
            _budget.RecordIncome(BudgetCategory.Utilities, utilityRevenue);
        }

        if (utilityUpkeep.Units != 0)
        {
            _budget.RecordExpense(BudgetCategory.Utilities, utilityUpkeep);
        }

        // 5) Road upkeep (per directed edge).
        Money roadUpkeep = _settings.RoadUpkeepPerEdge * _roads.EdgeCount;
        if (roadUpkeep.Units != 0)
        {
            _budget.RecordExpense(BudgetCategory.RoadUpkeep, roadUpkeep);
        }

        // 6) Settle the period into the treasury and report.
        (Money income, Money expenses) = _budget.Settle();
        _ledger.Post(new LedgerEntry(context.Tick, CityAgentId, BudgetCategory.Taxes, income - expenses));
        _events.Publish(new BudgetChangedEvent(context.Tick, _budget.Balance, income, expenses, devResidential));
    }

    private void ClearMarket(ResourceKind kind, float supply, float demand)
    {
        Market market = _markets[kind];
        market.SubmitSupply(supply);
        market.SubmitDemand(demand);
        market.Clear();
        _events.Publish(new MarketClearedEvent(kind, market.ClearingPrice, market.Supply, market.Demand));
        market.Reset();
    }

    private static Money TaxYield(Money baseYield, long developmentSum, float rate)
        => new((long)(baseYield.Units * (double)developmentSum * rate));
}
