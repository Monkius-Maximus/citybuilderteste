using CityBuilder.Events;
using CityBuilder.Events.Notifications;

namespace CityBuilder.Commands;

/// <summary>
/// Default single-player command processor: validate → execute → record → notify.
/// Successful, undoable commands are pushed to <see cref="CommandHistory"/>; every attempt
/// emits a <see cref="CommandExecutedEvent"/> so the UI/log can react. Redo re-runs the
/// command's <see cref="ICommand.Execute"/>.
/// </summary>
public sealed class CommandProcessor : ICommandProcessor
{
    private readonly ISimulationContext _context;
    private readonly CommandHistory _history;

    public CommandProcessor(ISimulationContext context, CommandHistory? history = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _history = history ?? new CommandHistory();
    }

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    /// <summary>Optional replay/telemetry recorder notified of every successful mutation.</summary>
    public ICommandRecorder? Recorder { get; set; }

    public CommandResult Submit(ICommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (!command.CanExecute(_context))
        {
            var rejected = CommandResult.Fail($"'{command.Name}' preconditions not met.");
            Notify(command, rejected);
            return rejected;
        }

        CommandResult result = command.Execute(_context);
        if (result.Success)
        {
            _history.Record(command);
            Recorder?.OnExecuted(command, _context.CurrentTick);
        }

        Notify(command, result);
        return result;
    }

    public bool Undo()
    {
        if (!_history.TryPopUndo(out ICommand command))
        {
            return false;
        }

        command.Undo(_context);
        _history.PushRedo(command);
        Recorder?.OnUndone(command, _context.CurrentTick);
        _context.Events.Publish(new CommandExecutedEvent($"Undo:{command.Name}", true, null));
        return true;
    }

    public bool Redo()
    {
        if (!_history.TryPopRedo(out ICommand command))
        {
            return false;
        }

        command.Execute(_context);
        _history.PushUndo(command);
        Recorder?.OnRedone(command, _context.CurrentTick);
        _context.Events.Publish(new CommandExecutedEvent($"Redo:{command.Name}", true, null));
        return true;
    }

    private void Notify(ICommand command, in CommandResult result)
        => _context.Events.Publish(new CommandExecutedEvent(command.Name, result.Success, result.Message));
}
