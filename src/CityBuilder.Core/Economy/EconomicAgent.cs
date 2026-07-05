namespace CityBuilder.Economy;

/// <summary>
/// A concrete funds-holding agent (city treasury, a company, a household). Provided as the
/// reusable implementation of the <see cref="IEconomicAgent"/> contract for when agent-level
/// balances are simulated; the city economy uses <see cref="Budget"/> for its treasury.
/// </summary>
public sealed class EconomicAgent : IEconomicAgent
{
    private Money _balance;

    public EconomicAgent(int agentId, Money initialBalance = default)
    {
        AgentId = agentId;
        _balance = initialBalance;
    }

    public int AgentId { get; }
    public Money Balance => _balance;

    public bool CanAfford(Money amount) => _balance >= amount;

    public bool TryDebit(Money amount)
    {
        if (_balance >= amount)
        {
            _balance -= amount;
            return true;
        }

        return false;
    }

    public void Credit(Money amount) => _balance += amount;
}
