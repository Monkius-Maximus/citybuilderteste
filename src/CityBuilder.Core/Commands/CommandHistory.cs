namespace CityBuilder.Commands;

/// <summary>
/// Undo/redo stacks. Recording a new command clears the redo stack (standard linear-history
/// semantics). Capacity is bounded so a long session doesn't retain every action forever.
/// </summary>
public sealed class CommandHistory
{
    private readonly List<ICommand> _undo = new();
    private readonly List<ICommand> _redo = new();
    private readonly int _capacity;

    public CommandHistory(int capacity = 256) => _capacity = Math.Max(1, capacity);

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Record(ICommand command)
    {
        _undo.Add(command);
        if (_undo.Count > _capacity)
        {
            _undo.RemoveAt(0); // drop oldest
        }

        _redo.Clear();
    }

    public bool TryPopUndo(out ICommand command) => TryPop(_undo, out command);

    public bool TryPopRedo(out ICommand command) => TryPop(_redo, out command);

    public void PushRedo(ICommand command) => _redo.Add(command);

    public void PushUndo(ICommand command) => _undo.Add(command);

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private static bool TryPop(List<ICommand> stack, out ICommand command)
    {
        if (stack.Count == 0)
        {
            command = null!;
            return false;
        }

        int last = stack.Count - 1;
        command = stack[last];
        stack.RemoveAt(last);
        return true;
    }
}
