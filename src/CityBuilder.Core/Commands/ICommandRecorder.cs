namespace CityBuilder.Commands;

/// <summary>
/// Optional observer of the command stream, invoked by the <see cref="CommandProcessor"/> on
/// every successful mutation. This is the recording half of replay: capture (tick, action) as
/// it happens, and re-applying the same stream on a fresh simulation reproduces the exact
/// state. A lockstep multiplayer host uses the same seam to broadcast commands to peers.
/// </summary>
public interface ICommandRecorder
{
    void OnExecuted(ICommand command, long tick);

    void OnUndone(ICommand command, long tick);

    void OnRedone(ICommand command, long tick);
}
