using CityBuilder.Commands;

namespace CityBuilder.Persistence;

/// <summary>What kind of player action a replay entry represents.</summary>
public enum ReplayEntryKind : byte
{
    Command = 0,
    Undo = 1,
    Redo = 2,
}

/// <summary>One recorded player action: what happened and on which simulation tick.</summary>
public readonly struct ReplayEntry
{
    public readonly long Tick;
    public readonly ReplayEntryKind Kind;

    /// <summary>The command for <see cref="ReplayEntryKind.Command"/> entries; null for undo/redo markers.</summary>
    public readonly ICommand? Command;

    public ReplayEntry(long tick, ReplayEntryKind kind, ICommand? command)
    {
        Tick = tick;
        Kind = kind;
        Command = command;
    }
}

/// <summary>
/// The ordered stream of player actions for one session. Together with the initial
/// <see cref="GameConfig"/> (seed included) and the same host bootstrap, replaying this log
/// reproduces the exact simulation state — the classic deterministic-replay design, and the
/// same data a lockstep multiplayer host would broadcast.
/// </summary>
public sealed class ReplayLog
{
    private readonly List<ReplayEntry> _entries = new();

    public IReadOnlyList<ReplayEntry> Entries => _entries;

    public int Count => _entries.Count;

    public void Add(in ReplayEntry entry) => _entries.Add(entry);

    public void Clear() => _entries.Clear();
}

/// <summary>
/// Plugs into <see cref="CommandProcessor.Recorder"/> and appends every successful player
/// action to a <see cref="ReplayLog"/>. Undo/redo are recorded as markers — on replay the
/// processor pops its own history, so no payload is needed.
/// </summary>
public sealed class ReplayRecorder : ICommandRecorder
{
    private readonly ReplayLog _log;

    public ReplayRecorder(ReplayLog log) => _log = log;

    public void OnExecuted(ICommand command, long tick)
        => _log.Add(new ReplayEntry(tick, ReplayEntryKind.Command, command));

    public void OnUndone(ICommand command, long tick)
        => _log.Add(new ReplayEntry(tick, ReplayEntryKind.Undo, null));

    public void OnRedone(ICommand command, long tick)
        => _log.Add(new ReplayEntry(tick, ReplayEntryKind.Redo, null));
}
