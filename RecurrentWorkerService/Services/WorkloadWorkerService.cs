using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace RecurrentWorkerService.Services;

internal class WorkloadWorkerService : IWorkerService
{
	private readonly ILogger<WorkloadWorkerService> _logger;
	private readonly Func<IWorkloadWorker> _workerFactory;
	private readonly WorkloadSchedule _schedule;
	private readonly WorkloadWorkerExecutionDelayCalculator _delayCalculator;
	private readonly Stopwatch _stopwatch;

	public WorkloadWorkerService(
		ILogger<WorkloadWorkerService> logger,
		Func<IWorkloadWorker> workerFactory,
		WorkloadSchedule schedule,
		WorkloadWorkerExecutionDelayCalculator delayCalculator)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_schedule = schedule;
		_delayCalculator = delayCalculator;
		_stopwatch = new Stopwatch();
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using var _ = _logger.BeginScope(Guid.NewGuid().ToString());
			_logger.LogDebug("Creating new Worker...");
			var worker = _workerFactory();

			_stopwatch.Restart();
			var isError = false;
			var workload = Workload.Zero;
			var delay = _schedule.PeriodFrom + _schedule.PeriodTo / 2;
			try
			{
				_logger.LogDebug($"[{worker}] Start");
				workload = await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
				_logger.LogDebug($"[{worker}] Success");
			}
			catch (Exception e)
			{
				_logger.LogError($"[{worker}] Fail: {e}");
				isError = true;
			}

			delay = TimeSpanExtensions.Max(_delayCalculator.Calculate(_schedule, delay, workload, isError) - _stopwatch.Elapsed, TimeSpan.Zero);
			_logger.LogDebug($"[{worker}] Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
		}
	}
}