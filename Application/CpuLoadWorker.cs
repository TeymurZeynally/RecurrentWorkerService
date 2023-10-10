using RecurrentWorkerService.Workers;
using System.Diagnostics;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class CpuLoadWorker : IWorkloadWorker
{
	private readonly ILogger<CpuLoadWorker> _logger;

	public CpuLoadWorker(ILogger<CpuLoadWorker> logger)
	{
		_logger = logger;
	}

	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Cpu load worker");
		_logger.LogInformation("Start");

		var random = new Random();

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(random.Next(4, 10)));
		var percentage = random.Next(30, 50);

		var list = new List<Task>();

		for (int i = 0; i < Environment.ProcessorCount; i++)
		{
			list.Add(Task.Run(() =>
			{
				var watch = new Stopwatch();
				watch.Start();
				while (!cts.Token.IsCancellationRequested)
				{
					if (watch.ElapsedMilliseconds > percentage)
					{
						Thread.Sleep(100 - percentage);
						watch.Reset();
						watch.Start();
					}
				}
			}, cancellationToken));
		}

		await Task.WhenAll(list.ToArray());
		_logger.LogInformation("End");

		return Workload.FromDoneItems(percentage, 50);
	}
}