using CityBuilder.Ecs;

namespace CityBuilder.Events.Notifications;

/// <summary>Raised when the traffic layer spawns a vehicle and assigns it a route.</summary>
public readonly struct VehicleSpawnedEvent : IEvent
{
    public readonly Entity Vehicle;
    public readonly int OriginNode;
    public readonly int DestinationNode;
    public readonly int RouteLength;

    public VehicleSpawnedEvent(Entity vehicle, int originNode, int destinationNode, int routeLength)
    {
        Vehicle = vehicle;
        OriginNode = originNode;
        DestinationNode = destinationNode;
        RouteLength = routeLength;
    }
}

/// <summary>Raised when a vehicle reaches the last node of its route (before it despawns).</summary>
public readonly struct VehicleArrivedEvent : IEvent
{
    public readonly Entity Vehicle;
    public readonly int DestinationNode;
    public readonly double TravelSeconds;

    public VehicleArrivedEvent(Entity vehicle, int destinationNode, double travelSeconds)
    {
        Vehicle = vehicle;
        DestinationNode = destinationNode;
        TravelSeconds = travelSeconds;
    }
}
