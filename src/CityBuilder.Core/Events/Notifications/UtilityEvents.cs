using CityBuilder.Networks;

namespace CityBuilder.Events.Notifications;

/// <summary>
/// Published each utility solve: supply vs demand and how much in-range demand was actually
/// served. The UI reads this for the power/water panels and brownout warnings.
/// </summary>
public readonly struct UtilityUpdatedEvent : IEvent
{
    public readonly NetworkType Kind;
    public readonly float Supply;
    public readonly float Demand;
    public readonly float ReachableDemand;
    public readonly float ServedDemand;
    public readonly int ServedConsumers;
    public readonly int ReachableConsumers;
    public readonly bool Brownout;

    public UtilityUpdatedEvent(
        NetworkType kind,
        float supply,
        float demand,
        float reachableDemand,
        float servedDemand,
        int servedConsumers,
        int reachableConsumers,
        bool brownout)
    {
        Kind = kind;
        Supply = supply;
        Demand = demand;
        ReachableDemand = reachableDemand;
        ServedDemand = servedDemand;
        ServedConsumers = servedConsumers;
        ReachableConsumers = reachableConsumers;
        Brownout = brownout;
    }
}
