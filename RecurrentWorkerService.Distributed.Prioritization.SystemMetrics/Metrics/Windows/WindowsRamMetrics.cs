using System.Runtime.InteropServices;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Windows;

public class WindowsRamMetrics : IRamMetrics
{
	public double GetUsedRam()
	{
		var memStatus = new MEMORYSTATUSEX();

		if (!WindowsNative.GlobalMemoryStatusEx(memStatus))
		{
			throw new Exception(Marshal.GetLastWin32Error().ToString());
		}

		return memStatus.dwMemoryLoad / 100d;
	}
}