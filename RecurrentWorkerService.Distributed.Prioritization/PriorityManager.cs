using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Prioritization.Aggregators;

namespace RecurrentWorkerService.Distributed.Prioritization;

internal class PriorityManager : IPriorityManager
{
	public PriorityManager(
		IComputedPriorityAggregator computedPriorityAggregator,
		IPriorityChangesAggregator priorityChangesAggregator,
		IPersistence persistence,
		ILogger<PriorityManager> logger)
	{
		_computedPriorityAggregator = computedPriorityAggregator;
		_priorityChangesAggregator = priorityChangesAggregator;
		_persistence = persistence;
		_waitForLockTimeout = TimeSpan.FromSeconds(3);
		_logger = logger;
	}

	public async Task WaitForExecutionOrderAsync(string identity, long revisionStart, CancellationToken cancellationToken)
	{
		var order = _computedPriorityAggregator.GetNodeOrder(identity);
		try
		{
			var cts = new CancellationTokenSource(_waitForLockTimeout);
			cancellationToken.Register(cts.Cancel);

			await _persistence.WaitForOrderAsync(order, identity, revisionStart, cts.Token).ConfigureAwait(false);
		}
		catch
		{
			_logger.LogWarning($"Wait for order {order} timeout exceeded for {identity}");
		}
	}

	public bool IsFirstInExecutionOrder(string identity, TimeSpan waitTime)
	{
		if(waitTime > _waitForLockTimeout)
		{
			return true;
		}

		return _computedPriorityAggregator.GetNodeOrder(identity) == 0;
	}

	public async Task ResetExecutionResultAsync(string identity, bool force, CancellationToken cancellationToken)
	{
		if (force || _computedPriorityAggregator.GetNodeOrder(identity) > 0)
		{
			await _priorityChangesAggregator.ResetPriorityAsync(identity, cancellationToken).ConfigureAwait(false);
		}
	}

	public async Task DecreaseExecutionPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		await _priorityChangesAggregator.DecreasePriorityAsync(identity, cancellationToken).ConfigureAwait(false);
	}

	private readonly IComputedPriorityAggregator _computedPriorityAggregator;
	private readonly IPriorityChangesAggregator _priorityChangesAggregator;
	private readonly IPersistence _persistence;
	private readonly TimeSpan _waitForLockTimeout;
	private readonly ILogger<PriorityManager> _logger;
}