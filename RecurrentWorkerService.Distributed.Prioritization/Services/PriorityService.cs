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
		IdentityPriorityIndicator[] identityPriorityIndicators,
		NodePriorityIndicator[] nodePriorityIndicators,
		ILogger<PriorityService> logger)
	{
		_persistence = persistence;
		_computedPriorityAggregator = computedPriorityAggregator;
		_priorityChangesAggregator = priorityChangesAggregator;
		_identityPriorityIndicators = identityPriorityIndicators;
		_nodePriorityIndicators = nodePriorityIndicators;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.WhenAll(
			CollectFailuresPriorityChanges(stoppingToken),
			CollectIdentityPriorityChanges(stoppingToken),
			CollectNodePriorityChanges(stoppingToken),
			CollectIdentityIndicatorsInfo(stoppingToken),
			CollectNodeIndicatorsInfo(stoppingToken));
	}

	private async Task CollectFailuresPriorityChanges(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				_computedPriorityAggregator.ResetPriorityInformation();
		
				_logger.LogDebug("Retrieving priority information");
				await foreach (var update in _persistence.WatchFailuresPriorityUpdates(cancellationToken))
				{
					_logger.LogDebug($"Received priority update {update.NodeId} {update.Identity} {update.Priority}");
					_computedPriorityAggregator.UpdateFailuresPriorityInformation(update);
				}
			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "Priority update connection problem. Retrying...");
			}
		}
	}

	private async Task CollectIdentityPriorityChanges(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				_computedPriorityAggregator.ResetPriorityInformation();

				_logger.LogDebug("Retrieving priority information");
				await foreach (var update in _persistence.WatchIdentityPriorityUpdates(cancellationToken))
				{
					_logger.LogDebug($"Received priority update {update.NodeId} {update.Identity} {update.Priority}");
					_computedPriorityAggregator.UpdateIdentityPriorityInformation(update);
				}
			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "Priority update connection problem. Retrying...");
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
			catch (Exception e)
			{
				_logger.LogWarning(e, "Node priority update connection problem. Retrying...");
			}
		}
	}

	private async Task CollectIdentityIndicatorsInfo(CancellationToken cancellationToken)
	{
		if (!_identityPriorityIndicators.Any())
		{
			return;
		}

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var indicators = _identityPriorityIndicators.Select(x => (x.Identity, x.GetMeasurement())).ToArray();
				await _priorityChangesAggregator.UpdateIdentityIndicatorPrioritiesAsync(indicators, cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "IdentityI indicators priority update connection problem. Retrying...");
			}
		}
	}

	private async Task CollectNodeIndicatorsInfo(CancellationToken cancellationToken)
	{
		if (!_nodePriorityIndicators.Any())
		{
			return;
		}

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var indicators = _nodePriorityIndicators.Select(x => x.GetMeasurement()).ToArray();
				await _priorityChangesAggregator.UpdateNodeIndicatorPrioritiesAsync(indicators, cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "Node indicators priority update connection problem. Retrying...");
			}
		}
	}

	private readonly IPersistence _persistence;
	private readonly IComputedPriorityAggregator _computedPriorityAggregator;
	private readonly IPriorityChangesAggregator _priorityChangesAggregator;
	private readonly IdentityPriorityIndicator[] _identityPriorityIndicators;
	private readonly NodePriorityIndicator[] _nodePriorityIndicators;
	private readonly ILogger<PriorityService> _logger;
}