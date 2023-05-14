using System.Net.NetworkInformation;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

public class NetworkMetrics : INetworkMetrics
{
	public long GetReceivedBytes()
	{
		return NetworkInterface.GetAllNetworkInterfaces().Max(x => x.GetIPStatistics().BytesReceived);
	}

	public long GetSentBytes()
	{
		return NetworkInterface.GetAllNetworkInterfaces().Max(x => x.GetIPStatistics().BytesSent);
	}
}