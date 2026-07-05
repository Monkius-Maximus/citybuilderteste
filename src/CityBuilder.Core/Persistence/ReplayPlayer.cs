namespace CityBuilder.Persistence;

/// <summary>
/// Re-applies a <see cref="ReplayLog"/> against a fresh simulation: run ticks until each
/// entry's recorded tick, then apply the action — exactly the cadence the original session
/// had. Because the simulation is deterministic (same seed, same bootstrap, same command
/// stream), the final state matches the original bit for bit (verify with
/// <see cref="StateChecksum"/>). This loop is also the skeleton of a lockstep client: replace
/// "read from log" with "read from network" and it is the same machine.
/// </summary>
public static class ReplayPlayer
{
    /// <summary>
    /// Play a log into <paramref name="sim"/>, which must be freshly constructed with the same
    /// config/seed and the same host bootstrap as the recorded session (definitions, scenario
    /// content, systems). Optionally keep simulating up to <paramref name="runToTick"/> after
    /// the last entry.
    /// </summary>
    public static void Play(GameSimulation sim, ReplayLog log, long? runToTick = null)
    {
        for (int i = 0; i < log.Count; i++)
        {
            ReplayEntry entry = log.Entries[i];

            while (sim.CurrentTick < entry.Tick)
            {
                sim.Step();
            }

            switch (entry.Kind)
            {
                case ReplayEntryKind.Command:
                    sim.Submit(entry.Command!);
                    break;
                case ReplayEntryKind.Undo:
                    sim.Undo();
                    break;
                case ReplayEntryKind.Redo:
                    sim.Redo();
                    break;
            }
        }

        if (runToTick.HasValue)
        {
            while (sim.CurrentTick < runToTick.Value)
            {
                sim.Step();
            }
        }
    }
}
