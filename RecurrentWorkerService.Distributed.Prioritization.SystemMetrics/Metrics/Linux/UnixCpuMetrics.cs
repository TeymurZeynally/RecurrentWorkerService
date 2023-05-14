namespace RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics.Linux;

public class UnixCpuMetrics : ICpuMetrics
{
	public double GetUsedCpu()
	{
		lock (_lockObject)
		{
			var stats = GetCpuStats();
			var prev = _lastStats;

			if (DateTimeOffset.UtcNow - _lastUpdateTime > TimeSpan.FromSeconds(1))
			{
				_lastStats = stats;
				_lastUpdateTime = DateTimeOffset.UtcNow;
			}

			var prevTotal = prev.Idle + prev.NonIdle;
			var total = stats.Idle + stats.NonIdle;

			var totald = total - prevTotal;
			var idled = stats.Idle - prev.Idle;

			return (totald - idled) / totald;
		}
	}

	private (double Idle, double NonIdle) GetCpuStats()
	{
		var stats = File.ReadAllLines("/proc/stat").First(x => x.StartsWith("cpu "));

		var pieces = stats.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		var user = double.Parse(pieces[1]);
		var nice = double.Parse(pieces[2]);
		var system = double.Parse(pieces[3]);
		var idle = double.Parse(pieces[4]);
		var ioWait = double.Parse(pieces[5]);
		var irq = double.Parse(pieces[6]);
		var softIrq = double.Parse(pieces[7]);
		var steal = double.Parse(pieces[8]);

		var totalIdle = idle + ioWait;
		var totalNonIdle = user + nice + system + irq + softIrq + steal;

		return (totalIdle, totalNonIdle);
	}

	private object _lockObject = new object();
	private DateTimeOffset _lastUpdateTime = DateTimeOffset.MinValue;
	private (double Idle, double NonIdle) _lastStats;
}