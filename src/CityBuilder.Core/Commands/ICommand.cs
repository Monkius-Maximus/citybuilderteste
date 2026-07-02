namespace CityBuilder.Commands;

/// <summary>
/// A reified player/AI action (build road, demolish, rezone, set tax...). Every mutation of
/// the world goes through a command. This buys us, for free: undo/redo (each command knows
/// how to reverse itself), a replayable action log, and deterministic lockstep multiplayer
/// (serialize commands, apply the identical stream on every client).
/// <para>
/// Commands capture their parameters at construction and apply them in <see cref="Execute"/>
/// against the supplied <see cref="ISimulationContext"/>; they should record whatever prior
/// state <see cref="Undo"/> needs during execution.
/// </para>
/// </summary>
public interface ICommand
{
    string Name { get; }

    /// <summary>Cheap validation before mutating anything (bounds, funds, legality).</summary>
    bool CanExecute(ISimulationContext context);

    CommandResult Execute(ISimulationContext context);

    /// <summary>Reverse the effects applied by <see cref="Execute"/>.</summary>
    void Undo(ISimulationContext context);
}
