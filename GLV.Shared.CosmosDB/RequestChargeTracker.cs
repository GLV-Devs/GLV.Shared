namespace GLV.Shared.CosmosDB;

public sealed class RequestChargeTracker(double previouslyStoredLocallyTrackedAllTime = 0)
{
    public double SessionTotal { get; private set; }
    public double LocallyTrackedAllTime { get; private set; } = previouslyStoredLocallyTrackedAllTime;

    public void Add(double charge)
    {
        SessionTotal += charge;
        LocallyTrackedAllTime += charge;
    }
}
