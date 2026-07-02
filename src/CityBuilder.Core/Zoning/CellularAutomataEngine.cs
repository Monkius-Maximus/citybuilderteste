using CityBuilder.Common;
using CityBuilder.Grid;

namespace CityBuilder.Zoning;

/// <summary>
/// Runs the zoning cellular automaton with double buffering: every rule reads from the
/// stable current state and writes into a scratch buffer, so a cell's update never sees
/// another cell's half-applied update within the same pass. After all cells are computed
/// the scratch buffer is copied back — this is what makes the automaton deterministic and
/// order-independent.
/// </summary>
public sealed class CellularAutomataEngine
{
    private readonly List<ICellularAutomatonRule> _rules = new();
    private ZoneCell[] _scratch = Array.Empty<ZoneCell>();

    public IReadOnlyList<ICellularAutomatonRule> Rules => _rules;

    public void AddRule(ICellularAutomatonRule rule) => _rules.Add(rule);

    /// <summary>Advance the automaton one generation over the zoning layer.</summary>
    public void Step(GridLayer<ZoneCell> zoning, IHeatMapProvider heat, DeterministicRandom rng)
    {
        if (_rules.Count == 0)
        {
            return;
        }

        Span<ZoneCell> current = zoning.AsSpan();
        if (_scratch.Length != current.Length)
        {
            _scratch = new ZoneCell[current.Length];
        }

        int w = zoning.Width;
        int h = zoning.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                var coord = new GridCoord(x, y);
                ZoneCell next = current[i];

                // Chain the rules: each transforms the cell produced by the previous one.
                for (int r = 0; r < _rules.Count; r++)
                {
                    next = _rules[r].Evaluate(coord, in next, zoning, heat, rng);
                }

                _scratch[i] = next;
            }
        }

        _scratch.AsSpan(0, current.Length).CopyTo(current);
    }
}
