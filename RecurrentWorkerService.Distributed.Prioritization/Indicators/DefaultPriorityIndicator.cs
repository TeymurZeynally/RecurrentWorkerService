namespace RecurrentWorkerService.Distributed.Prioritization.Indicators;

internal class DefaultPriorityIndicator : IPriorityIndicator
{
    public byte GetMeasurement()
    {
        return byte.MinValue;
    }
}