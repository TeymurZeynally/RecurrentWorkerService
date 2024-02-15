using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedRecurrentMultipleIterationWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedRecurrentMultipleIterationWorkerService> _logger;
	private readonly Func<IRecurrentWorker> _workerFactory;
	private readonly RecurrentWorkerExecutionDateCalculator _executionDateCalculator;
	private readonly IPersistence _persistence;
	private readonly RecurrentSchedule _schedule;
	private readonly IPriorityManager _priorityManager;
	private readonly string _identity;
	private readonly TimeSpan _iterationsMaxDuration;

	private DateTimeOffset _priortyAcquireTryDateTime = DateTimeOffset.MinValue;
	private long _priortyAcquireIterationNumber = 0;

	public DistributedRecurrentMultipleIterationWorkerService(
		ILogger<DistributedRecurrentMultipleIterationWorkerService> logger,
		Func<IRecurrentWorker> workerFactory,
		RecurrentSchedule schedule,
		RecurrentWorkerExecutionDateCalculator executionDateCalculator,
		IPersistence persistence,
		IPriorityManager priorityManager,
		string identity,
		TimeSpan iterationsMaxDuration)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDateCalculator = executionDateCalculator;
		_persistence = persistence;
		_schedule = schedule;
		_priorityManager = priorityManager;
		_identity = identity;
		_iterationsMaxDuration = iterationsMaxDuration;
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _priorityManager.ResetExecutionResultAsync(_identity, true, stoppingToken).ConfigureAwait(false);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var _ = _logger.BeginScope(_identity);

				_logger.LogDebug("Waiting for lock...");
				var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, stoppingToken).ConfigureAwait(false);
				if (string.IsNullOrEmpty(acquiredLock)) continue;
				_logger.LogDebug("Lock acquired");

				try
				{
					await ExecuteMultipleIterations(stoppingToken).ConfigureAwait(false);
				}
				finally
				{
					_logger.LogDebug("Releasing acquired lock...");
					await _persistence.ReleaseExecutionLockAsync(acquiredLock, stoppingToken).ConfigureAwait(false);
					_logger.LogDebug("Acquired lock released");
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Iteration error: {e}");
			}
		}
	}

	private async Task ExecuteMultipleIterations(CancellationToken stoppingToken)
	{
		var isFirstIteration = true;
	
		var startTimestamp = DateTimeOffset.UtcNow;

		// TODO: Add priprity Check
		while (!stoppingToken.IsCancellationRequested && DateTimeOffset.UtcNow - startTimestamp <= _iterationsMaxDuration)
		{
			var (nextN, nextExecutionDate) = _executionDateCalculator.CalculateNextExecutionDate(_schedule.Period, DateTimeOffset.UtcNow);
			var currentN = nextN - 1;

			if(currentN != _priortyAcquireIterationNumber)
			{
				_priortyAcquireIterationNumber = currentN;
				_priortyAcquireTryDateTime = DateTimeOffset.UtcNow;
			}

			if (!_priorityManager.IsFirstInExecutionOrder(_identity, DateTimeOffset.UtcNow - _priortyAcquireTryDateTime))
			{
				return;
			}

			_logger.LogDebug($"Checking is {currentN} succeeded...");
			var succeeded = isFirstIteration && (await _persistence.IsSucceededAsync(_identity, currentN, stoppingToken).ConfigureAwait(false)).Data;
			isFirstIteration = false;

			if (!succeeded)
			{
				_logger.LogDebug($"Starting {currentN} execution...");
				succeeded = await ExecuteWorker(stoppingToken).ConfigureAwait(false);

				if (succeeded && nextExecutionDate - DateTimeOffset.UtcNow >= TimeSpan.FromMilliseconds(1))
				{
					await _persistence.SucceededAsync(_identity, currentN, GetPersistentItemsLifetime(_schedule.Period), stoppingToken).ConfigureAwait(false);
					_logger.LogDebug($"Success key for {currentN} created");
				}
			}

			var delay = !succeeded && _schedule.RetryOnFailDelay.HasValue
				? _schedule.RetryOnFailDelay.Value
				: TimeSpanExtensions.Max(TimeSpan.Zero, nextExecutionDate - DateTimeOffset.UtcNow);

			_logger.LogDebug($"Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
		}
	}

	private async Task<bool> ExecuteWorker(CancellationToken stoppingToken)
	{
		try
		{
			_logger.LogDebug("Creating new Worker...");
			var worker = _workerFactory();

			_logger.LogDebug($"[{worker}] Start");
			await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
			_logger.LogDebug($"[{worker}] Success");
			await _priorityManager.ResetExecutionResultAsync(_identity, false, stoppingToken).ConfigureAwait(false);
			return true;
		}
		catch (Exception e)
		{
			_logger.LogError($"Fail: {e}");
			await _priorityManager.DecreaseExecutionPriorityAsync(_identity, stoppingToken).ConfigureAwait(false);
			return false;
		}
	}

	private TimeSpan GetPersistentItemsLifetime(TimeSpan period) => period * 3;
}