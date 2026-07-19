using CityBuilder.Economy;
using CityBuilder.Grid;
using CityBuilder.Networks;
using CityBuilder.Pathfinding;
using CityBuilder.Simulation;
using CityBuilder.Tests.Framework;

namespace CityBuilder.Tests.Tests;

public static class CoreSystemsTests
{
    // --- Pathfinding ---

    [TestCase]
    public static void AStar_FindsOptimalPathOnGrid()
    {
        var road = new FlowNetwork(NetworkType.Road);
        RoadGridBuilder.BuildGrid(road, new GridCoord(0, 0), new GridCoord(8, 8), edgeCost: 1f, capacity: 8);

        road.TryGetNodeAt(new GridCoord(0, 0), out NodeId start);
        road.TryGetNodeAt(new GridCoord(8, 8), out NodeId goal);

        var astar = new AStarPathfinder(Heuristics.Manhattan);
        var path = new List<int>();
        PathResult result = astar.FindPath(road, start.Value, goal.Value, path);

        Check.True(result.Found, "path found on connected grid");
        Check.Near(16.0, result.Cost, 0.001, "optimal cost = manhattan distance");
        Check.Equal(17, result.NodeCount, "17 nodes across 16 unit edges");
    }

    [TestCase]
    public static void AStar_UnreachableGoal_ReturnsNotFound()
    {
        var road = new FlowNetwork(NetworkType.Road);
        NodeId a = road.AddNode(new GridCoord(0, 0));
        NodeId b = road.AddNode(new GridCoord(5, 5)); // isolated, no edges

        var astar = new AStarPathfinder(Heuristics.Manhattan);
        PathResult result = astar.FindPath(road, a.Value, b.Value, new List<int>());
        Check.False(result.Found, "no path to an isolated node");
    }

    [TestCase]
    public static void Congestion_RaisesEffectiveCost()
    {
        var road = new FlowNetwork(NetworkType.Road);
        for (int x = 0; x < 6; x++)
        {
            road.AddNode(new GridCoord(x, 0));
        }
        for (int x = 0; x < 5; x++)
        {
            road.TryGetNodeAt(new GridCoord(x, 0), out NodeId from);
            road.TryGetNodeAt(new GridCoord(x + 1, 0), out NodeId to);
            road.Connect(from, to, 1f, capacity: 4, bidirectional: true);
        }

        road.TryGetNodeAt(new GridCoord(0, 0), out NodeId s);
        road.TryGetNodeAt(new GridCoord(5, 0), out NodeId g);

        var astar = new AStarPathfinder(Heuristics.Manhattan);
        var path = new List<int>();

        PathResult stat = astar.FindPath(road, s.Value, g.Value, path);

        var congestion = new CongestionWeightProvider(edgeCapacityHint: road.EdgeCount + 1);
        congestion.SetLoad(new EdgeId(0), 16f); // jam the first edge
        PathResult jammed = astar.FindPath(road, s.Value, g.Value, path, congestion);

        Check.True(jammed.Cost > stat.Cost, "congestion raises the effective route cost");
    }

    [TestCase]
    public static void DijkstraMap_ComputesDistances()
    {
        var road = new FlowNetwork(NetworkType.Road);
        RoadGridBuilder.BuildGrid(road, new GridCoord(0, 0), new GridCoord(6, 6), 1f, 8);
        road.TryGetNodeAt(new GridCoord(0, 0), out NodeId source);
        road.TryGetNodeAt(new GridCoord(3, 3), out NodeId probe);

        var map = new DijkstraMap();
        map.Build(road, new[] { source.Value });

        Check.Near(6.0, map.GetDistance(probe.Value), 0.001, "distance to (3,3) is 6");
        Check.Near(0.0, map.GetDistance(source.Value), 0.001, "source distance is 0");
    }

    // --- Grid & iso math ---

    [TestCase]
    public static void IsoProjection_RoundTripsThroughScreen()
    {
        var projector = new IsometricProjector(tileWidth: 64, tileHeight: 32);
        foreach (GridCoord cell in new[] { new GridCoord(0, 0), new GridCoord(3, 5), new GridCoord(10, 2) })
        {
            ScreenPoint screen = projector.GridToScreen(cell);
            GridCoord back = projector.ScreenToCell(screen);
            Check.Equal(cell, back, "grid -> screen -> grid round trip");
        }
    }

    [TestCase]
    public static void GridLayer_IndexerReadsWhatWasWritten()
    {
        var layer = new GridLayer<int>(MapLayer.Overlay, 10, 8);
        layer[new GridCoord(3, 4)] = 42;
        Check.Equal(42, layer[new GridCoord(3, 4)], "indexer round trip");
        Check.Equal(0, layer.GetOrDefault(new GridCoord(99, 99)), "out of bounds returns default");
    }

    // --- Money ---

    [TestCase]
    public static void Money_FormatsWithGlyphAndSeparators()
    {
        Check.Equal("§ 45,120", Money.FromWhole(45120).ToString(), "money display format");
    }

    [TestCase]
    public static void Money_ArithmeticIsExact()
    {
        Money sum = Money.FromWhole(100) + Money.FromWhole(50);
        Check.Equal(150L, sum.Units / 100, "addition");
        Check.True(Money.FromWhole(10) > Money.FromWhole(5), "comparison");
    }

    // --- Calendar ---

    [TestCase]
    public static void Calendar_MapsTicksToYearAndSeason()
    {
        Check.Equal(1, GameCalendar.YearOf(0), "new city is year 1");
        Check.Equal(Season.Spring, GameCalendar.SeasonOf(0), "starts in spring");
        Check.Equal(2, GameCalendar.YearOf(GameCalendar.TicksPerYear), "next year after a full year");
        Check.Equal(Season.Summer, GameCalendar.SeasonOf(GameCalendar.TicksPerSeason), "second season is summer");
    }

    // --- Simulation clock (framerate independence) ---

    [TestCase]
    public static void Clock_AccumulatesFixedTicks()
    {
        var clock = new SimulationClock(ticksPerSecond: 10);
        Check.Equal(0, clock.Advance(0.05), "half a tick worth => 0 ticks");
        Check.Equal(1, clock.Advance(0.05), "accumulated to a full tick => 1");
        Check.Equal(5, clock.Advance(0.5), "half a second => 5 ticks");
    }
}
