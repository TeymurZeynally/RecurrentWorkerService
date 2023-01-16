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

internal class DistributedWorkloadWorkerService : IDistributedWorkerService
{
	private readonly ILogger<DistributedWorkloadWorkerService> _logger;
	private readonly Func<IWorkloadWorker> _workerFactory;
	private readonly WorkloadWorkerExecutionDelayCalculator _executionDelayCalculator;
	private readonly IPersistence _persistence;
	private readonly WorkloadSchedule _schedule;
	private readonly IPriorityManager _priorityManager;
	private readonly string _identity;
	private readonly ActivitySource _activitySource;
	private readonly KeyValuePair<string, object?>[] _activitySourceTags;

	private long _revision;

	public DistributedWorkloadWorkerService(
		ILogger<DistributedWorkloadWorkerService> logger,
		Func<IWorkloadWorker> workerFactory,
		WorkloadSchedule schedule,
		WorkloadWorkerExecutionDelayCalculator executionDelayCalculator,
		IPersistence persistence,
		IPriorityManager priorityManager,
		string identity,
		long nodeId,
		ActivitySource activitySource)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDelayCalculator = executionDelayCalculator;
		_persistence = persistence;
		_schedule = schedule;
		_priorityManager = priorityManager;
		_identity = identity;
		_activitySource = activitySource;
		_activitySourceTags = new [] { new KeyValuePair<string, object?>("node", nodeId), new KeyValuePair<string, object?>("identity", identity) };
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: nameof(DistributedWorkloadWorkerService), tags: _activitySourceTags);
				using var _ = _logger.BeginScope(_identity);

				_logger.LogDebug($"Waiting for execution order...");
				await _priorityManager.WaitForExecutionOrderAsync(_identity, _revision, _schedule.PeriodTo * 3, stoppingToken);

				_logger.LogDebug("Waiting for lock...");
				var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, stoppingToken);
				if (string.IsNullOrEmpty(acquiredLock)) continue;
				_logger.LogDebug("Lock acquired");

				var delay = TimeSpan.Zero;
				var workloadDelay = TimeSpan.Zero;

				try
				{
					_logger.LogDebug("Retrieving current workload info...");
					var currentWorkloadResponse = await _persistence.GetCurrentWorkloadAsync(_identity, stoppingToken);
					var workloadInfo = currentWorkloadResponse?.Data;
					_revision = currentWorkloadResponse?.Revision ?? _revision;

					if (workloadInfo != null)
					{
						_logger.LogDebug("Calculating delays...");
						workloadDelay =  _executionDelayCalculator.Calculate(_schedule, workloadInfo.LastDelay, workloadInfo.Workload, workloadInfo.IsError);
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
						var workload = await ExecuteWorker(stoppingToken);
						stopWatch.Stop();

						workloadInfo = new WorkloadInfo
						{
							LastDelay = workloadDelay,
							Elapsed = stopWatch.Elapsed,
							ExecutionDate = DateTimeOffset.UtcNow,
							IsError = !workload.HasValue,
							Workload = workload ?? workloadInfo?.Workload ?? Workload.Full
						};

						activity?.AddTag("workload", workloadInfo.Workload);
						activity?.AddTag("last-delay", workloadInfo.LastDelay);

						_logger.LogDebug("Updating workload info...");
						var response = await _persistence.UpdateWorkloadAsync(_identity, workloadInfo, TimeSpan.FromDays(60), stoppingToken);
						_revision = response.Revision;

						delay = _executionDelayCalculator.Calculate(_schedule, workloadInfo.LastDelay, workloadInfo.Workload, workloadInfo.IsError);
						delay = TimeSpanExtensions.Max(delay - workloadInfo.Elapsed, TimeSpan.Zero);
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

	private async Task<Workload?> ExecuteWorker(CancellationToken stoppingToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, name: $"{nameof(DistributedWorkloadWorkerService)}.{nameof(ExecuteWorker)}", tags: _activitySourceTags);
		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			var workload = await worker.ExecuteAsync(stoppingToken);
			_logger.LogDebug($"[{worker}] Success with workload {workload}");
			await _priorityManager.ResetExecutionResultAsync(_identity, stoppingToken);
			return workload;
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
			await _priorityManager.DecreaseExecutionPriorityAsync(_identity, stoppingToken);
			return null;
		}
	}
}