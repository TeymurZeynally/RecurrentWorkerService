namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics;

public interface IMetricsFacade
{
	public double GetUsedCpu();

	double GetUsedRam();

	public long GetReceivedBytes();

	public long GetSentBytes();
}