using CityBuilder.Economy;
using CityBuilder.Events;
using CityBuilder.Events.Notifications;
using CityBuilder.Grid;
using CityBuilder.Simulation;
using CityBuilder.Zoning;

namespace CityBuilder.Population;

/// <summary>
/// Derives the city's demographics from developed zoning and refreshes the <see cref="DemandModel"/>
/// each population tick — then circulates wages/consumption through the sector accounts and
/// publishes the figures. It runs BEFORE the zoning system so that tick's cellular-automata
/// growth reads fresh demand (that ordering is set up in the composition root). Reads the tax
/// policy so higher taxes visibly cool demand. Pure data + events; no rendering, no RNG.
/// </summary>
public sealed class PopulationSystem : ISimulationSystem
{
    private readonly WorldMap _map;
    private readonly HeatMapRegistry _heat;
    private readonly ITaxPolicy _taxes;
    private readonly DemandModel _demand;
    private readonly IEventBus _events;
    private readonly int _interval;

    public PopulationSystem(
        WorldMap map,
        HeatMapRegistry heat,
        ITaxPolicy taxes,
        ILedger ledger,
        DemandModel demand,
        DemandSettings settings,
        IEventBus events,
        int tickInterval)
    {
        _map = map;
        _heat = heat;
        _taxes = taxes;
        _demand = demand;
        _events = events;
        _interval = Math.Max(1, tickInterval);
        Sectors = new SectorAccounts(ledger, settings);
    }

    /// <summary>Aggregate sector agents (households/commerce/industry) transacting via the ledger.</summary>
    public SectorAccounts Sectors { get; }

    public string Name => "Population";

    public int TickInterval => _interval;

    public void OnTick(in TickContext context)
    {
        long residentialDev = 0, commercialDev = 0, industrialDev = 0;
        float desirabilitySum = 0f;
        int zonedCount = 0;

        bool hasDesirability = _heat.TryGet(HeatMapKind.Desirability, out HeatMap desirability);

        int w = _map.Width;
        Span<ZoneCell> cells = _map.Zoning.AsSpan();
        for (int i = 0; i < cells.Length; i++)
        {
            ZoneCell c = cells[i];
            if (c.Type == ZoneType.None)
            {
                continue;
            }

            zonedCount++;
            if (hasDesirability)
            {
                desirabilitySum += desirability.Sample(new GridCoord(i % w, i / w));
            }

            switch (c.Type)
            {
                case ZoneType.Residential: residentialDev += c.DevelopmentLevel; break;
                case ZoneType.Commercial: commercialDev += c.DevelopmentLevel; break;
                case ZoneType.Industrial: industrialDev += c.DevelopmentLevel; break;
            }
        }

        float averageDesirability = zonedCount > 0 ? desirabilitySum / zonedCount : 0f;

        _demand.Update(
            residentialDev, commercialDev, industrialDev,
            averageDesirability,
            _taxes.GetRate(ZoneType.Residential),
            _taxes.GetRate(ZoneType.Commercial),
            _taxes.GetRate(ZoneType.Industrial));

        Sectors.Settle(context.Tick, _demand.Employed, _demand.CommercialJobs, _demand.IndustrialJobs);

        _events.Publish(new PopulationChangedEvent(context.Tick, _demand.Population, _demand.Jobs, _demand.EmploymentRate));
        _events.Publish(new DemandChangedEvent(_demand.Residential, _demand.Commercial, _demand.Industrial));
    }
}
