using CityBuilder.Networks;
using CityBuilder.Zoning;

namespace CityBuilder.Data;

// Concrete definition types. Kept as plain data holders (composition, not inheritance):
// each is a flat bag of parameters the factory reads. `init` accessors make them
// effectively immutable once loaded.

/// <summary>Static parameters for a placeable building. All identifiers are generic.</summary>
public sealed class BuildingDefinition : IDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Which zone this building can grow in.</summary>
    public ZoneType Category { get; set; } = ZoneType.None;

    /// <summary>Footprint in tiles.</summary>
    public int FootprintWidth { get; set; } = 1;
    public int FootprintHeight { get; set; } = 1;

    /// <summary>People/jobs the fully-grown building supports.</summary>
    public int MaxOccupancy { get; set; }

    /// <summary>Nuisance emitted into the pollution heat-map (arbitrary units).</summary>
    public float PollutionOutput { get; set; }

    public float PowerDemand { get; set; }
    public float WaterDemand { get; set; }
}

public enum VehicleClass : byte
{
    Passenger = 0,
    Freight = 1,
    Transit = 2,
    Service = 3,
}

/// <summary>Static parameters for a poolable, path-following vehicle. Generic identifiers only.</summary>
public sealed class VehicleDefinition : IDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public VehicleClass Class { get; set; } = VehicleClass.Passenger;

    /// <summary>Cells per second at free flow.</summary>
    public float MaxSpeed { get; set; } = 1f;

    /// <summary>Passengers or freight units carried.</summary>
    public int Capacity { get; set; }
}

/// <summary>Static parameters for a network element (road/rail/pipe) placed by infrastructure tools.</summary>
public sealed class InfrastructureDefinition : IDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public NetworkType Network { get; set; } = NetworkType.Road;

    /// <summary>Static per-tile traversal cost contributed to the graph.</summary>
    public float BaseCost { get; set; } = 1f;

    /// <summary>Throughput used by congestion models.</summary>
    public int Capacity { get; set; } = 32;
}
