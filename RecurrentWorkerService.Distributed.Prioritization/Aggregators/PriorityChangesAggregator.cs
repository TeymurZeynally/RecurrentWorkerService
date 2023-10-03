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
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken).ConfigureAwait(false);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken).ConfigureAwait(false);
	}

	public async Task ResetPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		_executionFailuresDictionary.TryAdd(identity, new ConcurrentBag<DateTimeOffset>());
		_executionFailuresDictionary[identity].Clear();
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken).ConfigureAwait(false);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken).ConfigureAwait(false);
	}

	public async Task UpdateIndicatorPriorities(byte[] indicators, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		if (_indicators.Count > _indicatorsCapacity)
		{
			_indicators.RemoveRange(0, _indicators.Count - _indicatorsCapacity);
		}

		_indicators.Add(indicators);
		var priority =  await _priorityCalculator.GetNodePriorityAsync(_indicators.ToArray(), cancellationToken).ConfigureAwait(false);
		await _persistence.UpdateNodePriorityAsync(priority, cancellationToken).ConfigureAwait(false);
	}

	private readonly IPersistence _persistence;
	private readonly IPriorityCalculator _priorityCalculator;
	private readonly KeyValuePair<string, object?>[] _activityNodeTags;
	private readonly ActivitySource _activitySource;

	private readonly ConcurrentDictionary<string, ConcurrentBag<DateTimeOffset>> _executionFailuresDictionary = new();
	private readonly List<byte[]> _indicators = new();
	private readonly int _indicatorsCapacity = 100;
}