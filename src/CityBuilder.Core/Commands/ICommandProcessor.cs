namespace CityBuilder.Commands;

/// <summary>
/// Central entry point for all world mutations. Validates and runs commands, records the
/// undoable ones, and emits telemetry events. A multiplayer host would additionally buffer
/// submitted commands and dispatch them on a scheduled tick so every peer executes the same
/// ordered stream — the interface is deliberately shaped to allow that later.
/// </summary>
public interface ICommandProcessor
{
    CommandResult Submit(ICommand command);

    bool CanUndo { get; }
    bool CanRedo { get; }

    bool Undo();
    bool Redo();
}
