using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Prioritization.Aggregators;
using RecurrentWorkerService.Distributed.Prioritization.Indicators;

namespace RecurrentWorkerService.Distributed.Prioritization.Services;

internal class PriorityService : BackgroundService
{
	public PriorityService(
		IPersistence persistence,
		IComputedPriorityAggregator computedPriorityAggregator,
		IPriorityChangesAggregator priorityChangesAggregator,
		IPriorityIndicator[] priorityIndicators,
		ILogger<PriorityService> logger)
	{
		_persistence = persistence;
		_computedPriorityAggregator = computedPriorityAggregator;
		_priorityChangesAggregator = priorityChangesAggregator;
		_priorityIndicators = priorityIndicators;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.WhenAll(
			CollectPriorityChanges(stoppingToken),
			CollectNodePriorityChanges(stoppingToken),
			CollectIndicatorsInfo(stoppingToken));
	}

	private async Task CollectPriorityChanges(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				_computedPriorityAggregator.ResetPriorityInformation();
		
				_logger.LogDebug("Retrieving priority information");
				await foreach (var update in _persistence.WatchPriorityUpdates(cancellationToken))
				{
					_logger.LogDebug($"Received priority update {update.NodeId} {update.Identity} {update.Priority}");
					_computedPriorityAggregator.UpdatePriorityInformation(update);
				}
			}
			catch (Exception)
			{
				_logger.LogWarning("Priority update connection problem. Retrying...");
			}
		}
	}

	private async Task CollectNodePriorityChanges(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				_computedPriorityAggregator.ResetNodePriorityInformation();

				_logger.LogDebug("Retrieving node priority information");
				await foreach (var update in _persistence.WatchNodePriorityUpdates(cancellationToken))
				{
					_logger.LogDebug($"Received node priority update {update.NodeId} {update.Priority}");
					_computedPriorityAggregator.UpdateNodePriorityInformation(update);
				}
			}
			catch (Exception)
			{
				_logger.LogWarning("Node priority update connection problem. Retrying...");
			}
		}
	}

	private async Task CollectIndicatorsInfo(CancellationToken cancellationToken)
	{
		if (!_priorityIndicators.Any())
		{
			return;
		}

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var indicators = _priorityIndicators.Select(x => x.GetMeasurement()).ToArray();
				await _priorityChangesAggregator.UpdateIndicatorPriorities(indicators, cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

			}
			catch (Exception)
			{
				_logger.LogWarning("Indicators priority update connection problem. Retrying...");
			}
		}
	}

	private readonly IPersistence _persistence;
	private readonly IComputedPriorityAggregator _computedPriorityAggregator;
	private readonly IPriorityChangesAggregator _priorityChangesAggregator;
	private readonly IPriorityIndicator[] _priorityIndicators;
	private readonly ILogger<PriorityService> _logger;
}