using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Persistence.Models;
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
	private readonly string _identity;

	public DistributedWorkloadWorkerService(
		ILogger<DistributedWorkloadWorkerService> logger,
		Func<IWorkloadWorker> workerFactory,
		WorkloadSchedule schedule,
		WorkloadWorkerExecutionDelayCalculator executionDelayCalculator,
		IPersistence persistence,
		string identity)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_executionDelayCalculator = executionDelayCalculator;
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

				var delay = TimeSpan.Zero;
				var workloadDelay = TimeSpan.Zero;

				try
				{
					_logger.LogDebug("Retrieving current workload info...");
					var workloadInfo = await _persistence.GetCurrentWorkloadAsync(_identity, stoppingToken);

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

						_logger.LogDebug("Updating workload info...");
						await _persistence.UpdateWorkloadAsync(_identity, workloadInfo, TimeSpan.FromDays(60), stoppingToken);

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
		_logger.LogDebug("Creating new Worker...");
		var worker = _workerFactory();

		try
		{
			_logger.LogDebug($"[{worker}] Start");
			var workload = await worker.ExecuteAsync(stoppingToken);
			_logger.LogDebug($"[{worker}] Success with workload {workload}");
			return workload;
		}
		catch (Exception e)
		{
			_logger.LogError($"[{worker}] Fail: {e}");
			return null;
		}
	}
}