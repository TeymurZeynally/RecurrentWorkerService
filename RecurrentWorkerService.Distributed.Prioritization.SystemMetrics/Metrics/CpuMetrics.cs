using System.Runtime.InteropServices;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Linux;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Windows;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

public class CpuMetrics : ICpuMetrics
{
	public CpuMetrics()
	{
		_metrics = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? new WindowsCpuMetrics()
			: new UnixCpuMetrics();
	}

	public double GetUsedCpu()
	{
		return _metrics.GetUsedCpu();
	}

	private readonly ICpuMetrics _metrics;
}