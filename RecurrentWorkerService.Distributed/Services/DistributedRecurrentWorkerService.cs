using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Schedules;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Services;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedRecurrentWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedRecurrentWorkerService> _logger;
	private readonly Func<IRecurrentWorker> _workerFactory;
	private readonly IExecutionDateCalculator _executionDateCalculator;
	private readonly IPersistence _persistence;
	private readonly DistributedRecurrentSchedule _schedule;
	private readonly string _identity;

	public DistributedRecurrentWorkerService(
		ILogger<DistributedRecurrentWorkerService> logger,
		Func<IRecurrentWorker> workerFactory,
		DistributedRecurrentSchedule schedule,
		IExecutionDateCalculator executionDateCalculator,
		IPersistence persistence,
		string identity)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDateCalculator = executionDateCalculator;
		_persistence = persistence;
		_schedule = schedule;
		_identity = identity;
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var _ = _logger.BeginScope(_identity);

				_logger.LogDebug("Waiting for lock...");
				var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, stoppingToken);
				if (string.IsNullOrEmpty(acquiredLock)) continue;

				_logger.LogDebug("Lock acquired");

				var (nextN, nextExecutionDate) =
					_executionDateCalculator.CalculateNextExecutionDate(_schedule.Period, DateTimeOffset.UtcNow);
				var currentN = nextN - 1;

				_logger.LogDebug($"Checking is {currentN} succeeded...");
				var succeeded = await _persistence.IsSucceededAsync(_identity, currentN, stoppingToken);

				if (!succeeded)
				{
					await ExecuteIteration(acquiredLock, currentN, nextExecutionDate, stoppingToken);
				}

				var delay = nextExecutionDate - DateTimeOffset.UtcNow;
				delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;

				_logger.LogDebug($"Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
				await Task.Delay(delay, stoppingToken);
			}
			catch (Exception e)
			{
				_logger.LogError($"Iteration error: {e}");
			}
		}
	}

	private async Task ExecuteIteration(string acquiredLock, long currentN, DateTimeOffset nextExecutionDate, CancellationToken stoppingToken)
	{
		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			await worker.ExecuteAsync(stoppingToken);
			_logger.LogDebug($"[{worker}] Success");
			await _persistence.SucceededAsync(_identity, currentN, (DateTimeOffset.UtcNow - nextExecutionDate) * 3, stoppingToken);
			_logger.LogDebug($"[{worker}] Success key created");
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
		}
		finally
		{
			_logger.LogDebug($"[{worker}] Releasing acquired lock...");
			await _persistence.ReleaseExecutionLockAsync(acquiredLock, stoppingToken);
			_logger.LogDebug($"[{worker}] Acquired lock released");
		}
	}
}