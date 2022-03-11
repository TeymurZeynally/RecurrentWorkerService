using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Prioritization.Providers;

namespace RecurrentWorkerService.Distributed.Prioritization.Services;

internal class PriorityService : BackgroundService
{
	public PriorityService(
		IPersistence persistence,
		IPriorityProvider priorityProvider,
		ILogger<PriorityService> logger)
	{
		_persistence = persistence;
		_priorityProvider = priorityProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_priorityProvider.Reset();
			try
			{
				_logger.LogDebug("Retrieving priority information");
				await foreach (var update in _persistence.WatchPriorityUpdates(stoppingToken))
				{
					_logger.LogDebug($"Received priority update {update.NodeId} {update.Identity} {update.Priority}");
					_priorityProvider.UpdatePriorityInformation(update);
				}
			}
			catch (Exception)
			{
				_logger.LogWarning("Priority update connection problem. Retrying...");
			}
		}
	}

	private readonly IPersistence _persistence;
	private readonly ILogger<PriorityService> _logger;
	private readonly IPriorityProvider _priorityProvider;
}