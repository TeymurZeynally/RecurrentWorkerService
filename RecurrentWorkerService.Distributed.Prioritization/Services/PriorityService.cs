using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;
using RecurrentWorkerService.Distributed.Prioritization.Providers;

namespace RecurrentWorkerService.Distributed.Prioritization.Services;

internal class PriorityService : BackgroundService
{
	public PriorityService(
		IPersistence persistence,
		IRecurrentExecutionDelayCalculator executionDelayCalculator,
		IPriorityProvider priorityProvider,
		ILogger<PriorityService> logger)
	{
		_persistence = persistence;
		_executionDelayCalculator = executionDelayCalculator;
		_priorityProvider = priorityProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogDebug("Retrieving priority information");
			var priorities = await _persistence.GetAllPrioritiesAsync(stoppingToken);

			_priorityProvider.UpdatePriorityInformation(priorities);

			var delay = _executionDelayCalculator.CalculateExecutionDelay(TimeSpan.FromSeconds(5), DateTimeOffset.UtcNow);
			await Task.Delay(delay, stoppingToken);
		}
	}

	private readonly IPersistence _persistence;
	private readonly IRecurrentExecutionDelayCalculator _executionDelayCalculator;
	private readonly ILogger<PriorityService> _logger;
	private readonly IPriorityProvider _priorityProvider;
}