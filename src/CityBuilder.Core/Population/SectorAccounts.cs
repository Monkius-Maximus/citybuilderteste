using CityBuilder.Economy;

namespace CityBuilder.Population;

/// <summary>
/// Aggregate money circulation between the city's economic sectors, realised with concrete
/// <see cref="EconomicAgent"/>s posting to the shared <see cref="ILedger"/> — the roadmap's
/// per-company/household agents at pool granularity (cheap and deterministic). Each economic
/// tick: employers pay wages to households, households consume at commerce, commerce restocks
/// from industry. Transfers are atomic (debit+credit) and skipped if the payer can't afford
/// them, so the ledger always balances and no money is created or destroyed.
/// </summary>
public sealed class SectorAccounts
{
    private readonly ILedger _ledger;
    private readonly DemandSettings _settings;

    public SectorAccounts(ILedger ledger, DemandSettings settings)
    {
        _ledger = ledger;
        _settings = settings;

        Households = new EconomicAgent(EconomicAgentIds.Households, Money.FromWhole(5_000));
        Commerce = new EconomicAgent(EconomicAgentIds.Commerce, Money.FromWhole(20_000));
        Industry = new EconomicAgent(EconomicAgentIds.Industry, Money.FromWhole(20_000));
    }

    public EconomicAgent Households { get; }
    public EconomicAgent Commerce { get; }
    public EconomicAgent Industry { get; }

    public void Settle(long tick, long employed, long commercialJobs, long industrialJobs)
    {
        long totalJobs = commercialJobs + industrialJobs;
        Money wages = _settings.WagePerWorker * employed;

        // Employers pay wages, split by their share of jobs.
        if (totalJobs > 0 && wages.Units > 0)
        {
            var commercialWages = new Money(wages.Units * commercialJobs / totalJobs);
            Money industrialWages = wages - commercialWages;
            Transfer(Commerce, Households, commercialWages, tick);
            Transfer(Industry, Households, industrialWages, tick);
        }

        // Households consume at commerce; commerce restocks from industry.
        var consumption = new Money((long)(wages.Units * _settings.ConsumptionFraction));
        Transfer(Households, Commerce, consumption, tick);

        var wholesale = new Money((long)(consumption.Units * _settings.WholesaleFraction));
        Transfer(Commerce, Industry, wholesale, tick);
    }

    private void Transfer(EconomicAgent from, EconomicAgent to, Money amount, long tick)
    {
        if (amount.Units <= 0 || !from.TryDebit(amount))
        {
            return;
        }

        to.Credit(amount);
        _ledger.Post(new LedgerEntry(tick, from.AgentId, BudgetCategory.Other, -amount));
        _ledger.Post(new LedgerEntry(tick, to.AgentId, BudgetCategory.Other, amount));
    }
}
