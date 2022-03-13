using System.Collections.Concurrent;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal class PriorityChangesAggregator : IPriorityChangesAggregator
{
	public PriorityChangesAggregator(IPersistence persistence, IPriorityCalculator priorityCalculator)
	{
		_persistence = persistence;
		_priorityCalculator = priorityCalculator;
	}

	public async Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken)
	{
		_executionFailuresDictionary.TryAdd(identity, new ConcurrentBag<DateTimeOffset>());
		_executionFailuresDictionary[identity].Add(DateTimeOffset.UtcNow);
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken);
	}

	public async Task ResetPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		_executionFailuresDictionary.TryAdd(identity, new ConcurrentBag<DateTimeOffset>());
		_executionFailuresDictionary[identity].Clear();
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresDictionary[identity].ToArray(), cancellationToken);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken);
	}

	public async Task UpdateIndicatorPriorities(byte[] indicators, CancellationToken cancellationToken)
	{
		if (_indicators.Count > _indicatorsCapacity)
		{
			_indicators.RemoveRange(0, _indicators.Count - _indicatorsCapacity);
		}

		_indicators.Add(indicators);
		var priority =  await _priorityCalculator.GetNodePriorityAsync(_indicators.ToArray(), cancellationToken);
		await _persistence.UpdateNodePriorityAsync(priority, cancellationToken);
	}

	private readonly IPersistence _persistence;
	private readonly IPriorityCalculator _priorityCalculator;

	private readonly ConcurrentDictionary<string, ConcurrentBag<DateTimeOffset>> _executionFailuresDictionary = new();
	private readonly List<byte[]> _indicators = new();
	private readonly int _indicatorsCapacity = 100;
}