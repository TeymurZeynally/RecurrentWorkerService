using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedRecurrentWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedRecurrentWorkerService> _logger;
	private readonly Func<IRecurrentWorker> _workerFactory;
	private readonly RecurrentWorkerExecutionDateCalculator _executionDateCalculator;
	private readonly IPersistence _persistence;
	private readonly RecurrentSchedule _schedule;
	private readonly IPriorityManager _priorityManager;
	private readonly string _identity;
	private readonly ActivitySource _activitySource;
	private readonly KeyValuePair<string, object?>[] _activitySourceTags;

	private long _revision;

	public DistributedRecurrentWorkerService(
		ILogger<DistributedRecurrentWorkerService> logger,
		Func<IRecurrentWorker> workerFactory,
		RecurrentSchedule schedule,
		RecurrentWorkerExecutionDateCalculator executionDateCalculator,
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
		await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: nameof(DistributedRecurrentWorkerService), tags: _activitySourceTags);
				using var _ = _logger.BeginScope(_identity);

				_logger.LogDebug($"Waiting for execution order...");
				await _priorityManager.WaitForExecutionOrderAsync(_identity, _revision, GetPersistentItemsLifetime(_schedule.Period), stoppingToken);

				_logger.LogDebug("Waiting for lock...");
				var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, stoppingToken);
				if (string.IsNullOrEmpty(acquiredLock)) continue;

				_logger.LogDebug("Lock acquired");

				var (nextN, nextExecutionDate) = _executionDateCalculator.CalculateNextExecutionDate(_schedule.Period, DateTimeOffset.UtcNow);
				var delay = TimeSpan.Zero;

				try
				{
					var retryExecution = await ExecuteIteration(nextN - 1, stoppingToken);
					if (!retryExecution)
					{
						delay = TimeSpanExtensions.Max(TimeSpan.Zero, nextExecutionDate - DateTimeOffset.UtcNow);
					}
				}
				finally
				{
					_logger.LogDebug("Releasing acquired lock...");
					await _persistence.ReleaseExecutionLockAsync(acquiredLock, stoppingToken);
					_logger.LogDebug("Acquired lock released");
				}

				activity?.Dispose();

				_logger.LogDebug($"Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
				await Task.Delay(delay, stoppingToken);
			}
			catch (Exception e)
			{
				_logger.LogError($"Iteration error: {e}");
			}
		}
	}

	private async Task<bool> ExecuteIteration(long currentN, CancellationToken stoppingToken)
	{
		_logger.LogDebug($"Checking is {currentN} succeeded...");

		var succeededResult = await _persistence.IsSucceededAsync(_identity, currentN, stoppingToken);

		if (succeededResult.Data)
		{
			_logger.LogDebug($"Iteration {currentN} is succeeded...");
			_revision = succeededResult.Revision;
			return false;
		}

		_logger.LogDebug($"Starting {currentN} execution...");
		var succeeded = await ExecuteWorker(stoppingToken);

		if (succeeded)
		{
			var result = await _persistence.SucceededAsync(_identity, currentN, GetPersistentItemsLifetime(_schedule.Period), stoppingToken);
			_revision = result.Revision;
			_logger.LogDebug($"Success key for {currentN} created");
		}
		else if (_schedule.RetryOnFailDelay.HasValue)
		{
			await Task.Delay(_schedule.RetryOnFailDelay.Value, stoppingToken);
			return true;
		}

		return false;
	}


	private async Task<bool> ExecuteWorker(CancellationToken stoppingToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: $"{nameof(DistributedRecurrentWorkerService)}.{nameof(ExecuteWorker)}", tags: _activitySourceTags);
		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			await worker.ExecuteAsync(stoppingToken);
			_logger.LogDebug($"[{worker}] Success");
			await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken);
			return true;
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
			await _priorityManager.DecreaseExecutionPriorityAsync(_identity, stoppingToken);
			return false;
		}
	}

	private TimeSpan GetPersistentItemsLifetime(TimeSpan period) => period * 3;
}