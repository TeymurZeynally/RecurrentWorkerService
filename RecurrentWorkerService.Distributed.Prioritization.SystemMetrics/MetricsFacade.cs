using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics;

public class MetricsFacade : IMetricsFacade
{
	private readonly ICpuMetrics _cpuMetrics;
	private readonly IRamMetrics _ramMetrics;
	private readonly INetworkMetrics _networkMetrics;

	public MetricsFacade(ICpuMetrics cpuMetrics, IRamMetrics ramMetrics, INetworkMetrics networkMetrics)
	{
		_cpuMetrics = cpuMetrics;
		_ramMetrics = ramMetrics;
		_networkMetrics = networkMetrics;
	}

	public double GetUsedCpu()
	{
		return _cpuMetrics.GetUsedCpu();
	}

	public double GetUsedRam()
	{
		return _ramMetrics.GetUsedRam();
	}

	public long GetReceivedBytes()
	{
		return _networkMetrics.GetReceivedBytes();
	}

	public long GetSentBytes()
	{
		return _networkMetrics.GetSentBytes();
	}
}