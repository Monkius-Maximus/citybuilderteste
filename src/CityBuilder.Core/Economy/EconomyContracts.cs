using CityBuilder.Simulation;
using CityBuilder.Zoning;

namespace CityBuilder.Economy;

// =============================================================================
//  ECONOMY — CONTRACTS ONLY (deliberately unimplemented in this phase).
//
//  Per the architecture brief we are NOT building the economy yet — only the
//  seams where the economic math will plug into the agents and the simulation.
//  Every type here is an interface (or a data struct). A concrete, tick-driven
//  economy system will implement these later without touching the rest of the core.
// =============================================================================

/// <summary>Anything that can hold funds and settle transactions (city treasury, a company, a household).</summary>
public interface IEconomicAgent
{
    int AgentId { get; }
    Money Balance { get; }

    bool CanAfford(Money amount);

    /// <summary>Attempt to debit funds; returns false if the agent cannot afford it.</summary>
    bool TryDebit(Money amount);

    void Credit(Money amount);
}

/// <summary>City treasury: income vs. expenses per accounting period, plus loans/bonds later.</summary>
public interface IBudget
{
    Money Balance { get; }
    Money ProjectedIncome { get; }
    Money ProjectedExpenses { get; }

    void RecordIncome(BudgetCategory category, Money amount);
    void RecordExpense(BudgetCategory category, Money amount);
}

/// <summary>Append-only record of transactions for auditing, graphs and save/replay.</summary>
public interface ILedger
{
    void Post(in LedgerEntry entry);
    Money BalanceFor(int agentId);
}

/// <summary>
/// Supply/demand for a tradable resource (labour, goods, power, water). Prices emerge from
/// the gap between supply and demand; agents read the clearing price to make decisions.
/// </summary>
public interface IMarket
{
    ResourceKind Resource { get; }
    Money ClearingPrice { get; }
    float Supply { get; }
    float Demand { get; }

    void SubmitSupply(float quantity);
    void SubmitDemand(float quantity);
}

/// <summary>Tax rates by zone category. Player-tunable; commands mutate it via this contract.</summary>
public interface ITaxPolicy
{
    /// <summary>Rate in [0,1] for a zone category.</summary>
    float GetRate(ZoneType category);

    void SetRate(ZoneType category, float rate);
}

/// <summary>
/// The top-level economy simulation system. It will implement <see cref="ISimulationSystem"/>
/// and, each (glacial) tick, clear markets, apply taxes, pay upkeep and settle agents.
/// Declared here as the integration contract; implemented in a later milestone.
/// </summary>
public interface IEconomySystem : ISimulationSystem
{
    IBudget CityBudget { get; }
    ITaxPolicy Taxes { get; }

    IMarket GetMarket(ResourceKind resource);
}

// --- Supporting data types (structs/enums) for the contracts above ---

public enum BudgetCategory : byte
{
    Taxes = 0,
    RoadUpkeep = 1,
    Utilities = 2,
    Services = 3,
    Loans = 4,
    Other = 5,
}

public enum ResourceKind : byte
{
    Labour = 0,
    Goods = 1,
    Power = 2,
    Water = 3,
    Freight = 4,
}

/// <summary>A single posted transaction.</summary>
public readonly struct LedgerEntry
{
    public readonly long Tick;
    public readonly int AgentId;
    public readonly BudgetCategory Category;
    public readonly Money Amount; // positive = credit, negative = debit

    public LedgerEntry(long tick, int agentId, BudgetCategory category, Money amount)
    {
        Tick = tick;
        AgentId = agentId;
        Category = category;
        Amount = amount;
    }
}
