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
		while (!stoppingToken.IsCancellationRequested)
		{
			using var _ = _logger.BeginScope(Guid.NewGuid().ToString());

			_logger.LogDebug("Creating new Worker...");
			var worker = _workerFactory();

			var isError = false;
			try
			{
				_logger.LogDebug($"[{worker}] Start");
				await worker.ExecuteAsync(stoppingToken);
				_logger.LogDebug($"[{worker}] Success");
			}
			catch (Exception e)
			{
				_logger.LogError($"[{worker}] Fail: {e}");
				isError = true;
			}

			var delay = _delayCalculator.Calculate(_schedule, isError);
			_logger.LogDebug($"[{worker}] Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
			await Task.Delay(delay, stoppingToken);
		}
	}
}