using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Services;

internal class CronWorkerService : IWorkerService
{
	private readonly ILogger<CronWorkerService> _logger;
	private readonly Func<ICronWorker> _workerFactory;
	private readonly CronSchedule _schedule;
	private readonly CronWorkerExecutionDelayCalculator _delayCalculator;

	public CronWorkerService(
		ILogger<CronWorkerService> logger,
		Func<ICronWorker> workerFactory,
		CronSchedule schedule,
		CronWorkerExecutionDelayCalculator delayCalculator)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_schedule = schedule;
		_delayCalculator = delayCalculator;
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var isError = false;

		while (!stoppingToken.IsCancellationRequested)
		{
			using var _ = _logger.BeginScope(Guid.NewGuid().ToString());
			var now = DateTimeOffset.UtcNow;
			var delay = _delayCalculator.Calculate(_schedule, isError);

			_logger.LogDebug("Next execution will be after {Delay:g} at {NextExecutionTimestamp:O}", delay, now + delay);

			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);

			try
			{
				_logger.LogDebug("Creating new Worker...");
				var worker = _workerFactory();

				_logger.LogDebug("[{Worker}] Execution start", worker);
				await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
				_logger.LogDebug("[{Worker}] Execution succeeded", worker);
				isError = false;

			}
			catch (Exception e)
			{
				_logger.LogError(e, "Execution failed");
				isError = true;
			}
		}
	}
}