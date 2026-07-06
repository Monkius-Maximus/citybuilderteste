namespace CityBuilder.Economy;

/// <summary>
/// Stable ids for the aggregate economic actors that share the city <see cref="Ledger"/>.
/// The city treasury and the three sector pools each get a fixed id so their running balances
/// never collide. Per-company/household simulation would allocate ids above these.
/// </summary>
public static class EconomicAgentIds
{
    public const int City = 0;
    public const int Households = 1;
    public const int Commerce = 2;
    public const int Industry = 3;

    /// <summary>First id available for individually-simulated agents.</summary>
    public const int FirstDynamicId = 16;
}
