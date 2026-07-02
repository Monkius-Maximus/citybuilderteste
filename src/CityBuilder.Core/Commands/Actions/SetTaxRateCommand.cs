using CityBuilder.Economy;
using CityBuilder.Zoning;

namespace CityBuilder.Commands.Actions;

/// <summary>
/// Change the tax rate for a zone category. Demonstrates a command acting on the ECONOMY
/// CONTRACT (<see cref="ITaxPolicy"/>) without depending on any concrete economy — the
/// policy is injected. Captures the previous rate on execute so undo is exact.
/// </summary>
public sealed class SetTaxRateCommand : ICommand
{
    private readonly ITaxPolicy _policy;
    private readonly ZoneType _category;
    private readonly float _newRate;

    private float _previousRate;

    public SetTaxRateCommand(ITaxPolicy policy, ZoneType category, float newRate)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _category = category;
        _newRate = newRate;
    }

    public string Name => $"SetTax({_category},{_newRate:0.##})";

    public bool CanExecute(ISimulationContext context)
        => _newRate >= 0f && _newRate <= 1f && _category != ZoneType.None;

    public CommandResult Execute(ISimulationContext context)
    {
        _previousRate = _policy.GetRate(_category);
        _policy.SetRate(_category, _newRate);
        return CommandResult.Succeeded($"{_category} tax {_previousRate:0.##} -> {_newRate:0.##}.");
    }

    public void Undo(ISimulationContext context) => _policy.SetRate(_category, _previousRate);
}
