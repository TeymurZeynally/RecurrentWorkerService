using System.Collections.Concurrent;
using System.Diagnostics;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal class PriorityChangesAggregator : IPriorityChangesAggregator
{
	public PriorityChangesAggregator(IPersistence persistence, IPriorityCalculator priorityCalculator, long nodeId, ActivitySource activitySource)
	{
		_persistence = persistence;
		_priorityCalculator = priorityCalculator;
		_activitySource = activitySource;
		_activityNodeTags = new[] { new KeyValuePair<string, object?>("node", nodeId) };
	}

	public async Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		_executionFailuresDictionary.TryAdd(identity, new ConcurrentBag<DateTimeOffset>());
		_executionFailuresDictionary[identity].Add(DateTimeOffset.UtcNow);
		var priority = await _priorityCalculator.GetFailuresPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken);
		await _persistence.UpdateFailurePriorityAsync(identity, priority, cancellationToken);
	}

	public async Task ResetPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		_executionFailuresDictionary.TryAdd(identity, new ConcurrentBag<DateTimeOffset>());
		_executionFailuresDictionary[identity].Clear();
		var priority = await _priorityCalculator.GetFailuresPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken);
		await _persistence.UpdateFailurePriorityAsync(identity, priority, cancellationToken);
	}

	public async Task UpdateIdentityIndicatorPrioritiesAsync((string Identity, byte Measurement)[] indicators, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		foreach (var group in indicators.GroupBy(x => x.Identity))
		{
			var priority = await _priorityCalculator.GetIdentityPriorityAsync(group.Select(x => x.Measurement).ToArray(), cancellationToken);
			await _persistence.UpdateIdentityPriorityAsync(group.Key, priority, cancellationToken);
		}
	}

	public async Task UpdateNodeIndicatorPrioritiesAsync(byte[] indicators, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		var priority = await _priorityCalculator.GetNodePriorityAsync(indicators, cancellationToken);
		await _persistence.UpdateNodePriorityAsync(priority, cancellationToken);
	}


	private readonly IPersistence _persistence;
	private readonly IPriorityCalculator _priorityCalculator;
	private readonly KeyValuePair<string, object?>[] _activityNodeTags;
	private readonly ActivitySource _activitySource;

	private readonly ConcurrentDictionary<string, ConcurrentBag<DateTimeOffset>> _executionFailuresDictionary = new();
}