using CityBuilder.Economy;

namespace CityBuilder.Events.Notifications;

/// <summary>Published each economic settle: the treasury balance and the period's net flows.</summary>
public readonly struct BudgetChangedEvent : IEvent
{
    public readonly long Tick;
    public readonly Money Balance;
    public readonly Money Income;
    public readonly Money Expenses;

    /// <summary>Rough population proxy (sum of residential development).</summary>
    public readonly long Population;

    public BudgetChangedEvent(long tick, Money balance, Money income, Money expenses, long population)
    {
        Tick = tick;
        Balance = balance;
        Income = income;
        Expenses = expenses;
        Population = population;
    }
}

/// <summary>Published when a resource market clears, with the price and the supply/demand behind it.</summary>
public readonly struct MarketClearedEvent : IEvent
{
    public readonly ResourceKind Resource;
    public readonly Money Price;
    public readonly float Supply;
    public readonly float Demand;

    public MarketClearedEvent(ResourceKind resource, Money price, float supply, float demand)
    {
        Resource = resource;
        Price = price;
        Supply = supply;
        Demand = demand;
    }
}
