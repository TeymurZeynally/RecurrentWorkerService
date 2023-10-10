using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedCronWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedCronWorkerService> _logger;
	private readonly Func<ICronWorker> _workerFactory;
	private readonly CronWorkerExecutionDateCalculator _executionDateCalculator;
	private readonly IPersistence _persistence;
	private readonly CronSchedule _schedule;
	private readonly IPriorityManager _priorityManager;
	private readonly string _identity;
	private readonly ActivitySource _activitySource;
	private readonly KeyValuePair<string, object?>[] _activitySourceTags;

	private long _revision;

	public DistributedCronWorkerService(
		ILogger<DistributedCronWorkerService> logger,
		Func<ICronWorker> workerFactory,
		CronSchedule schedule,
		CronWorkerExecutionDateCalculator executionDateCalculator,
		IPersistence persistence,
		IPriorityManager priorityManager,
		string identity,
		long nodeId,
		ActivitySource activitySource)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDateCalculator = executionDateCalculator;
		_persistence = persistence;
		_schedule = schedule;
		_priorityManager = priorityManager;
		_identity = identity;
		_activitySource = activitySource;
		_activitySourceTags = new[] { new KeyValuePair<string, object?>("node", nodeId), new KeyValuePair<string, object?>("identity", identity) };
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var currentN = long.MinValue;
		var retryLimit = DateTimeOffset.UtcNow;
		var retryExecution = false;

		await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken).ConfigureAwait(false);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var _ = _logger.BeginScope(_identity);

				if (!retryExecution || retryLimit < DateTimeOffset.UtcNow)
				{
					var (nextN, nextExecutionDate) = _executionDateCalculator.CalculateNextExecutionDate(_schedule.CronExpression, DateTimeOffset.UtcNow);
					var delay = TimeSpanExtensions.Max(TimeSpan.Zero, nextExecutionDate - DateTimeOffset.UtcNow);
					_logger.LogDebug($"Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
					await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
					currentN = nextN;
					retryLimit = _executionDateCalculator.CalculateNextExecutionDate(_schedule.CronExpression, DateTimeOffset.UtcNow).ExecutionDate;
				}

				using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: nameof(DistributedCronWorkerService), tags: _activitySourceTags);

				_logger.LogDebug($"Waiting for execution order...");
				await _priorityManager.WaitForExecutionOrderAsync(_identity, _revision, GetPersistentItemsLifetime(retryLimit), stoppingToken).ConfigureAwait(false);

				_logger.LogDebug($"Waiting for lock for...");
				var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, stoppingToken).ConfigureAwait(false);
				if (string.IsNullOrEmpty(acquiredLock)) continue;

				try
				{
					retryExecution = await ExecuteIteration(currentN, retryLimit, stoppingToken).ConfigureAwait(false);
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

	private async Task<bool> ExecuteIteration(long currentN, DateTimeOffset nextExecutionDate, CancellationToken stoppingToken)
	{
		_logger.LogDebug($"Checking is {currentN} succeeded...");
		var succeededResult = await _persistence.IsSucceededAsync(_identity, currentN, stoppingToken).ConfigureAwait(false);

		if (succeededResult.Data)
		{
			_logger.LogDebug($"Iteration {currentN} is succeeded...");
			_revision = succeededResult.Revision;
			return false;
		}

		_logger.LogDebug($"Starting {currentN} execution...");
		var succeeded = await ExecuteWorker(stoppingToken).ConfigureAwait(false);

		if (succeeded)
		{
			var result =  await _persistence.SucceededAsync(_identity, currentN, GetPersistentItemsLifetime(nextExecutionDate), stoppingToken).ConfigureAwait(false);
			_revision = result.Revision;
			_logger.LogDebug($"Success key for {currentN} created");
		}
		else if (_schedule.RetryOnFailDelay.HasValue)
		{
			await Task.Delay(_schedule.RetryOnFailDelay.Value, stoppingToken).ConfigureAwait(false);
			return true;
		}

		return false;
	}

	private async Task<bool> ExecuteWorker(CancellationToken stoppingToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: $"{nameof(DistributedCronWorkerService)}.{nameof(ExecuteWorker)}", tags: _activitySourceTags);

		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
			_logger.LogDebug($"[{worker}] Success");
			await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken).ConfigureAwait(false);

			return true;
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
			await _priorityManager.DecreaseExecutionPriorityAsync(_identity, stoppingToken).ConfigureAwait(false);
			return false;
		}
	}

	private TimeSpan GetPersistentItemsLifetime(DateTimeOffset nextExecutionDate) => (DateTimeOffset.UtcNow - nextExecutionDate) * 3;
}