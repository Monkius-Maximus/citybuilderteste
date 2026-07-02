using CityBuilder.Common;
using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>
/// A single transition rule for the zoning cellular automaton. Given a cell's current
/// state, its neighbourhood (via the read-only <paramref name="source"/> buffer) and the
/// surrounding pressures (<paramref name="heat"/>), it returns the cell's next state.
/// Rules are pure functions of their inputs — that keeps the CA deterministic and the
/// rule set freely composable (add crime/pollution/land-value rules without touching the engine).
/// </summary>
public interface ICellularAutomatonRule
{
    string Name { get; }

    ZoneCell Evaluate(
        GridCoord cell,
        in ZoneCell current,
        GridLayer<ZoneCell> source,
        IHeatMapProvider heat,
        DeterministicRandom rng);
}
