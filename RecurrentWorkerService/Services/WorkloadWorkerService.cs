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
		var workloadDependedDelay = _schedule.PeriodFrom + _schedule.PeriodTo / 2;

		while (!stoppingToken.IsCancellationRequested)
		{
			using var _ = _logger.BeginScope(Guid.NewGuid().ToString());

			var workload = Workload.Zero;
			var isError = false;

			try
			{
				_logger.LogDebug("Creating new Worker...");
				var worker = _workerFactory();

				_stopwatch.Restart();

				_logger.LogDebug("[{Worker}] Execution start", worker);
				workload = await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
				_logger.LogDebug("[{Worker}] Execution succeeded with workload {Workload}", worker, workload.Value);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Execution failed");
				isError = true;
			}

			workloadDependedDelay = _delayCalculator.Calculate(_schedule, workloadDependedDelay, workload, isError);

			var delay = TimeSpanExtensions.Max(workloadDependedDelay - _stopwatch.Elapsed, TimeSpan.Zero);

			_logger.LogDebug("Next execution will be after {Delay:g} at {NextExecutionTimestamp:O}", delay, DateTimeOffset.UtcNow + delay);
			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
		}
	}
}
