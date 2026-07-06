namespace CityBuilder.Events.Notifications;

/// <summary>Published each population tick: the headline demographic figures for the UI.</summary>
public readonly struct PopulationChangedEvent : IEvent
{
    public readonly long Tick;
    public readonly long Population;
    public readonly long Jobs;
    public readonly float EmploymentRate;

    public PopulationChangedEvent(long tick, long population, long jobs, float employmentRate)
    {
        Tick = tick;
        Population = population;
        Jobs = jobs;
        EmploymentRate = employmentRate;
    }
}

/// <summary>Published with the RCI demand each population tick — drives the demand bars UI.</summary>
public readonly struct DemandChangedEvent : IEvent
{
    public readonly float Residential;
    public readonly float Commercial;
    public readonly float Industrial;

    public DemandChangedEvent(float residential, float commercial, float industrial)
    {
        Residential = residential;
        Commercial = commercial;
        Industrial = industrial;
    }
}
