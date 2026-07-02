using CityBuilder.Ecs;
using CityBuilder.Grid;

namespace CityBuilder.Events.Notifications;

/// <summary>Raised once when a fresh city/world has been created.</summary>
public readonly struct CityInitializedEvent : IEvent
{
    public readonly int Width;
    public readonly int Height;
    public readonly ulong Seed;

    public CityInitializedEvent(int width, int height, ulong seed)
    {
        Width = width;
        Height = height;
        Seed = seed;
    }
}

/// <summary>
/// Aggregate summary published after each zoning/CA pass. The UI's RCI demand bars and
/// population readouts observe this instead of scanning the grid themselves.
/// </summary>
public readonly struct ZoningUpdatedEvent : IEvent
{
    public readonly long Tick;
    public readonly int DevelopedCells;
    public readonly int ChangedCells;

    public ZoningUpdatedEvent(long tick, int developedCells, int changedCells)
    {
        Tick = tick;
        DevelopedCells = developedCells;
        ChangedCells = changedCells;
    }
}

/// <summary>Raised when the building factory places a structure on the map.</summary>
public readonly struct BuildingConstructedEvent : IEvent
{
    public readonly Entity Building;
    public readonly string DefinitionId;
    public readonly GridCoord Cell;

    public BuildingConstructedEvent(Entity building, string definitionId, GridCoord cell)
    {
        Building = building;
        DefinitionId = definitionId;
        Cell = cell;
    }
}

/// <summary>Raised when something is demolished at a cell.</summary>
public readonly struct StructureDemolishedEvent : IEvent
{
    public readonly GridCoord Cell;

    public StructureDemolishedEvent(GridCoord cell) => Cell = cell;
}

/// <summary>
/// Telemetry/undo-log breadcrumb emitted by the command processor for every command it
/// runs. Useful for an on-screen action log, analytics, and multiplayer echo.
/// </summary>
public readonly struct CommandExecutedEvent : IEvent
{
    public readonly string CommandName;
    public readonly bool Success;
    public readonly string? Message;

    public CommandExecutedEvent(string commandName, bool success, string? message)
    {
        CommandName = commandName;
        Success = success;
        Message = message;
    }
}
