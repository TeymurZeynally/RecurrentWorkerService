using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedWorkloadMultipleIterationWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedWorkloadMultipleIterationWorkerService> _logger;
	private readonly Func<IWorkloadWorker> _workerFactory;
	private readonly WorkloadWorkerExecutionDelayCalculator _executionDelayCalculator;
	private readonly IPersistence _persistence;
	private readonly WorkloadSchedule _schedule;
	private readonly IPriorityManager _priorityManager;
	private readonly string _identity;
	private readonly TimeSpan _iterationsMaxDuration;

	private DateTimeOffset _priortyAcquireTryDateTime = DateTimeOffset.MinValue;
	private DateTimeOffset _priortyAcquireIdentityExecutionDateTime = DateTimeOffset.MinValue;

	public DistributedWorkloadMultipleIterationWorkerService(
		ILogger<DistributedWorkloadMultipleIterationWorkerService> logger,
		Func<IWorkloadWorker> workerFactory,
		WorkloadSchedule schedule,
		WorkloadWorkerExecutionDelayCalculator executionDelayCalculator,
		IPersistence persistence,
		IPriorityManager priorityManager,
		string identity,
		TimeSpan iterationsMaxDuration)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDelayCalculator = executionDelayCalculator;
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
		var startTimestamp = DateTimeOffset.UtcNow;

		var workloadInfo = default(WorkloadInfo);
		var delay = TimeSpan.Zero;
		var workloadDelay = TimeSpan.Zero;

		while (!stoppingToken.IsCancellationRequested && DateTimeOffset.UtcNow - startTimestamp <= _iterationsMaxDuration)
		{
			if (workloadInfo?.ExecutionDate != _priortyAcquireIdentityExecutionDateTime)
			{
				_priortyAcquireIdentityExecutionDateTime = workloadInfo?.ExecutionDate ?? DateTimeOffset.MinValue;
				_priortyAcquireTryDateTime = DateTimeOffset.UtcNow;
			}

			if (!_priorityManager.IsFirstInExecutionOrder(_identity, DateTimeOffset.UtcNow - _priortyAcquireTryDateTime))
			{
				return;
			}

			workloadInfo ??= (await _persistence.GetCurrentWorkloadAsync(_identity, stoppingToken).ConfigureAwait(false))?.Data;

			if (workloadInfo != null)
			{
				_logger.LogDebug("Calculating delays...");
				workloadDelay = _executionDelayCalculator.Calculate(_schedule, workloadInfo.LastDelay, workloadInfo.Workload, workloadInfo.IsError);
				var waitDelay = DateTimeOffset.UtcNow - workloadInfo.ExecutionDate;
				delay = workloadDelay - waitDelay - workloadInfo.Elapsed;
				_logger.LogDebug($"Delays: workload delay: {workloadDelay:g}; wait delay: {waitDelay:g}; execution delay: {delay:g} ");
			}
			else
			{
				_logger.LogDebug("There is no workload info...");
			}

			if (delay <= TimeSpan.Zero)
			{
				_logger.LogDebug("Starting execution...");
				var stopWatch = new Stopwatch();
				stopWatch.Start();
				var workload = await ExecuteWorker(stoppingToken).ConfigureAwait(false);
				stopWatch.Stop();

				workloadInfo = new WorkloadInfo
				{
					LastDelay = workloadDelay,
					Elapsed = stopWatch.Elapsed,
					ExecutionDate = DateTimeOffset.UtcNow,
					IsError = !workload.HasValue,
					Workload = workload ?? workloadInfo?.Workload ?? Workload.Full
				};

				delay = _executionDelayCalculator.Calculate(_schedule, workloadInfo.LastDelay, workloadInfo.Workload, workloadInfo.IsError);
				delay = TimeSpanExtensions.Max(delay - workloadInfo.Elapsed, TimeSpan.Zero);

				if (delay >= TimeSpan.FromMilliseconds(1))
				{
					_logger.LogDebug("Updating workload info...");
					await _persistence.UpdateWorkloadAsync(_identity, workloadInfo, TimeSpan.FromDays(60), stoppingToken).ConfigureAwait(false);
				}
			}

			_logger.LogDebug($"Next execution will be after {delay:g} at {DateTimeOffset.UtcNow + delay:O}");
			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
		}
	}

	private async Task<Workload?> ExecuteWorker(CancellationToken stoppingToken)
	{
		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			var workload = await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
			_logger.LogDebug($"[{worker}] Success with workload {workload}");
			await _priorityManager.ResetExecutionResultAsync(_identity, false, stoppingToken).ConfigureAwait(false);
			return workload;
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
			await _priorityManager.DecreaseExecutionPriorityAsync(_identity, stoppingToken).ConfigureAwait(false);
			return null;
		}
	}
}