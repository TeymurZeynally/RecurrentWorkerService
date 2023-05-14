using System.Diagnostics;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Windows;

public class WindowsCpuMetrics : ICpuMetrics
{
	public WindowsCpuMetrics()
	{
		_processorTotal = new PerformanceCounter("Processor", "% Processor Time", "_Total");
	}

	public double GetUsedCpu()
	{
		return _processorTotal.NextValue() / 100d;
	}

	private readonly PerformanceCounter _processorTotal;
}