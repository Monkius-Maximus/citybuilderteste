namespace CityBuilder.Networks;

/// <summary>
/// The distinct transport/utility layers. Each is its own graph — a road junction and a
/// water main at the same tile are different nodes in different networks.
/// </summary>
public enum NetworkType : byte
{
    Road = 0,
    Rail = 1,
    WaterPipe = 2,
    PowerLine = 3,
    Sewage = 4,
}
