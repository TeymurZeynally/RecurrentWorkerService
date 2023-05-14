using System.Runtime.InteropServices;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Linux;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Windows;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

public class RamMetrics : IRamMetrics
{
	public RamMetrics()
	{
		_metrics = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? new WindowsRamMetrics()
			: new UnixRamMetrics();
	}

	public double GetUsedRam()
	{
		return _metrics.GetUsedRam();
	}

	private readonly IRamMetrics _metrics;
}