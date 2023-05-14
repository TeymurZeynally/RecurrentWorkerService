using System.Text.RegularExpressions;

namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Linux;

public class UnixRamMetrics : IRamMetrics
{
	public double GetUsedRam()
	{
		var dictionary = _lineRegex.Matches(File.ReadAllText("/proc/meminfo"))
			.Cast<Match>()
			.ToDictionary(match => match.Groups["name"].Value.TrimStart(), match => ulong.Parse(match.Groups["value"].Value));

		var total = (double)dictionary["MemTotal"];
		var used = (double)total - dictionary["MemFree"];

		return used / total;
	}

	private readonly Regex _lineRegex = new Regex(@"^(?<name>[^:]+):\s+(?<value>\d+) kB", RegexOptions.Multiline | RegexOptions.Compiled);
}