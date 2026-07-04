namespace CityBuilder.Economy;

/// <summary>
/// City treasury. Income and expenses accumulate over an accounting period; <see cref="Settle"/>
/// applies the net to the balance and starts a fresh period. Per-category totals are kept for
/// the budget panel. Integer money throughout → deterministic.
/// </summary>
public sealed class Budget : IBudget
{
    private Money _balance;
    private Money _periodIncome;
    private Money _periodExpenses;

    private readonly Dictionary<BudgetCategory, Money> _incomeByCategory = new();
    private readonly Dictionary<BudgetCategory, Money> _expenseByCategory = new();

    public Budget(Money startingBalance) => _balance = startingBalance;

    public Money Balance => _balance;
    public Money ProjectedIncome => _periodIncome;
    public Money ProjectedExpenses => _periodExpenses;

    public void RecordIncome(BudgetCategory category, Money amount)
    {
        _periodIncome += amount;
        _incomeByCategory[category] = GetOrZero(_incomeByCategory, category) + amount;
    }

    public void RecordExpense(BudgetCategory category, Money amount)
    {
        _periodExpenses += amount;
        _expenseByCategory[category] = GetOrZero(_expenseByCategory, category) + amount;
    }

    public Money IncomeOf(BudgetCategory category) => GetOrZero(_incomeByCategory, category);
    public Money ExpenseOf(BudgetCategory category) => GetOrZero(_expenseByCategory, category);

    /// <summary>Apply the period's net to the balance and reset the period. Returns (income, expenses).</summary>
    public (Money Income, Money Expenses) Settle()
    {
        Money income = _periodIncome;
        Money expenses = _periodExpenses;

        _balance += income - expenses;

        _periodIncome = Money.Zero;
        _periodExpenses = Money.Zero;
        _incomeByCategory.Clear();
        _expenseByCategory.Clear();

        return (income, expenses);
    }

    private static Money GetOrZero(Dictionary<BudgetCategory, Money> map, BudgetCategory key)
        => map.TryGetValue(key, out Money m) ? m : Money.Zero;
}
