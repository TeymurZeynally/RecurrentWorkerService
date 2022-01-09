using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Schedules;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Services;

internal class DistributedRecurrentWorkerHostedService : BackgroundService
{
	private readonly ILogger<DistributedRecurrentWorkerHostedService> _logger;
	private readonly Func<IRecurrentWorker> _workerFactory;
	private readonly IExecutionDateCalculator _executionDateCalculator;
	private readonly IPersistence _persistence;
	private readonly DistributedRecurrentSchedule _schedule;
	private readonly string _identity;

	public DistributedRecurrentWorkerHostedService(
		ILogger<DistributedRecurrentWorkerHostedService> logger,
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

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var now = DateTimeOffset.UtcNow;
		var (currentN, _) = _executionDateCalculator.CalculateCurrentExecutionDate(_schedule.Period, now);
		var (_, nextExecutionDate) = _executionDateCalculator.CalculateNextExecutionDate(_schedule.Period, now);

		var succeededIterations = new HashSet<int>(_schedule.ExecutionCount);

		while (!stoppingToken.IsCancellationRequested)
		{
			using var scope = _logger.BeginScope($"{_identity}-{currentN}");
			_logger.LogDebug("Starting execution");
			await ExecuteAllIterationOfSchedule(succeededIterations, currentN, stoppingToken);
			_logger.LogDebug($"Executed. Succeeded count: {succeededIterations.Count}");

			now = DateTimeOffset.UtcNow;
			if (nextExecutionDate <= now)
			{
				(currentN, _) = _executionDateCalculator.CalculateCurrentExecutionDate(_schedule.Period, now);
				(_, nextExecutionDate) = _executionDateCalculator.CalculateNextExecutionDate(_schedule.Period, now);
				succeededIterations.Clear();
			}

			if (succeededIterations.Count == _schedule.ExecutionCount)
			{
				await Task.Delay(nextExecutionDate - now, stoppingToken);
			}
		}
	}

	private async Task ExecuteAllIterationOfSchedule(ISet<int> succeededIterations, long scheduleIndex, CancellationToken stoppingToken)
	{
		var maxIterations = _schedule.ExecutionCount;
		for (int i = 0; i < maxIterations && !stoppingToken.IsCancellationRequested; i++)
		{
			using var scope = _logger.BeginScope($"{_identity}-{scheduleIndex}-{i}");
			if (succeededIterations.Contains(i)) continue;
			_logger.LogDebug("Checking iteration");

			var succeeded = await _persistence.IsSucceededAsync(_identity, scheduleIndex, i);
			if (succeeded)
			{
				_logger.LogDebug("Iteration already succeeded");
				succeededIterations.Add(i);
				continue;
			}

			if (await ExecuteIteration(scheduleIndex, i, stoppingToken))
			{
				succeededIterations.Add(i);
			}
		}
	}

	private async Task<bool> ExecuteIteration(long scheduleIndex, int iteration, CancellationToken stoppingToken)
	{
		var succeeded = false;
		_logger.LogDebug("Getting lock for iteration");
		var acquiredLock = await _persistence.AcquireExecutionLockAsync(_identity, scheduleIndex, iteration);
		if (!string.IsNullOrEmpty(acquiredLock))
		{
			_logger.LogDebug("Lock for iteration acquired");
			_logger.LogDebug("Creating new Worker...");
			var worker = _workerFactory();

			try
			{
				using var scope = _logger.BeginScope($"{_identity}-{scheduleIndex}-{iteration}-{worker}");
				_logger.LogDebug($"Start");
				await worker.ExecuteAsync(stoppingToken);
				_logger.LogDebug("Success");
				await _persistence.SucceededAsync(_identity, scheduleIndex, iteration);
				succeeded = true;
				_logger.LogDebug("Execution of iteration succeeded");
			}
			catch (Exception e)
			{
				_logger.LogError($"Fail: {e}");
			}

			await _persistence.ReleaseExecutionLockAsync(acquiredLock);
		}

		return succeeded;
	}
}