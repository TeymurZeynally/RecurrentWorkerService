namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

public interface INetworkMetrics
{
	public long GetReceivedBytes();

	public long GetSentBytes();
}