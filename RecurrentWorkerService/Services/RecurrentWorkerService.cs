using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Services;

internal class RecurrentWorkerService : IWorkerService
{
	private readonly ILogger<RecurrentWorkerService> _logger;
	private readonly Func<IRecurrentWorker> _workerFactory;
	private readonly RecurrentSchedule _schedule;
	private readonly RecurrentWorkerExecutionDelayCalculator _delayCalculator;
	private readonly Stopwatch _stopwatch;

	public RecurrentWorkerService(
		ILogger<RecurrentWorkerService> logger,
		Func<IRecurrentWorker> workerFactory,
		RecurrentSchedule schedule,
		RecurrentWorkerExecutionDelayCalculator delayCalculator)
	{
		_logger = logger;
		_workerFactory = workerFactory;
		_schedule = schedule;
		_delayCalculator = delayCalculator;
		_stopwatch = new Stopwatch();
	}

	public async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using var _ = _logger.BeginScope(Guid.NewGuid().ToString());

			var isError = false;
			try
			{
				_logger.LogDebug("Creating new Worker...");
				var worker = _workerFactory();

				_stopwatch.Restart();

				_logger.LogDebug("[{Worker}] Execution start", worker);
				await worker.ExecuteAsync(stoppingToken).ConfigureAwait(false);
				_logger.LogDebug("[{Worker}] Execution succeeded", worker);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Execution failed");
				isError = true;
			}

			var now = DateTimeOffset.UtcNow;
			var delay = _delayCalculator.Calculate(_schedule, _stopwatch.Elapsed, isError);

			_logger.LogDebug("Next execution will be after {Delay:g} at {NextExecutionTimestamp:O}", delay, now + delay);
			await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
		}
	}
}