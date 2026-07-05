namespace CityBuilder.Economy;

/// <summary>
/// Append-only transaction record with per-agent running balances. Bounded so a long game
/// doesn't retain every transaction forever (the running balances are kept regardless).
/// </summary>
public sealed class Ledger : ILedger
{
    private readonly List<LedgerEntry> _entries = new();
    private readonly Dictionary<int, Money> _balances = new();
    private readonly int _capacity;

    public Ledger(int capacity = 4096) => _capacity = Math.Max(1, capacity);

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    public void Post(in LedgerEntry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > _capacity)
        {
            _entries.RemoveAt(0);
        }

        _balances[entry.AgentId] = BalanceFor(entry.AgentId) + entry.Amount;
    }

    public Money BalanceFor(int agentId) => _balances.TryGetValue(agentId, out Money m) ? m : Money.Zero;
}
