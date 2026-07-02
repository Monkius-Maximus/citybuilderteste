namespace CityBuilder.Zoning;

/// <summary>The demarcation a player paints onto land. Deliberately generic.</summary>
public enum ZoneType : byte
{
    None = 0,
    Residential = 1,
    Commercial = 2,
    Industrial = 3,

    /// <summary>Civic/utility footprint (parks, plants, service buildings).</summary>
    Civic = 4,
}

/// <summary>Allowed building bulk within a zone; drives the growth ceiling.</summary>
public enum ZoneDensity : byte
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}
