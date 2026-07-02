using CityBuilder.Grid;

namespace CityBuilder.Networks;

/// <summary>
/// Convenience helper that wires a rectangular block of cells into a 4-connected road grid
/// (bidirectional edges). Useful for tests, procedural generation and demos; the player-facing
/// path to build roads remains <c>BuildRoadCommand</c>.
/// </summary>
public static class RoadGridBuilder
{
    public static void BuildGrid(
        FlowNetwork network,
        GridCoord min,
        GridCoord max,
        float edgeCost = 1f,
        int capacity = 8)
    {
        for (int y = min.Y; y <= max.Y; y++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                NodeId node = network.AddNode(new GridCoord(x, y));

                if (x < max.X)
                {
                    NodeId east = network.AddNode(new GridCoord(x + 1, y));
                    network.Connect(node, east, edgeCost, capacity, bidirectional: true);
                }

                if (y < max.Y)
                {
                    NodeId south = network.AddNode(new GridCoord(x, y + 1));
                    network.Connect(node, south, edgeCost, capacity, bidirectional: true);
                }
            }
        }
    }
}
