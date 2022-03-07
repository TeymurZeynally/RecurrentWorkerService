using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;
using RecurrentWorkerService.Distributed.Prioritization.Providers;

namespace RecurrentWorkerService.Distributed.Prioritization;

internal class PriorityManager : IPriorityManager
{
	public PriorityManager(
		IPriorityProvider priorityProvider,
		IPriorityCalculator priorityCalculator,
		IPersistence persistence,
		ILogger<PriorityManager> logger)
	{
		_priorityProvider = priorityProvider;
		_priorityCalculator = priorityCalculator;
		_persistence = persistence;
		_waitForLockTimeout = TimeSpan.FromSeconds(3);
		_logger = logger;
	}

	public async Task WaitForExecutionOrderAsync(string identity, long revisionStart, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		var priorities = _priorityProvider.GetPrioritiesAsc(identity);
		var priority = _priorityProvider.GetPriority(identity);

		var order = Math.Max(0, Array.IndexOf(priorities, priority));

		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine("[||||||||||||||||||||||||||||||||||]");
		Console.WriteLine($"PRIORITIES: [{string.Join(' ', priorities)}]  PRIORITY: {priority} ORDER: {order}");
		Console.WriteLine("[||||||||||||||||||||||||||||||||||]");
		Console.ResetColor();

		try
		{
			var cts = new CancellationTokenSource(_waitForLockTimeout);
			await _persistence.WaitForOrderAsync(order, identity, revisionStart, cts.Token);
		}
		catch
		{
			_logger.LogWarning($"Wait for order {order} timeout exceeded for {identity}");
		}
	}

	public async Task ResetPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		EnsureKeyExists(identity);
		_executionFailuresMap[identity].Clear();
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresMap[identity].ToArray(), cancellationToken);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken);

	}

	public async Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken)
	{
		EnsureKeyExists(identity);
		_executionFailuresMap[identity].Add(DateTimeOffset.UtcNow);
		var priority = await _priorityCalculator.GetPriorityAsync(_executionFailuresMap[identity].ToArray(), cancellationToken);
		await _persistence.UpdatePriorityAsync(identity, priority, cancellationToken); ;
	}

	private void EnsureKeyExists(string key)
	{
		if (!_executionFailuresMap.ContainsKey(key))
		{
			_executionFailuresMap.TryAdd(key, new ConcurrentBag<DateTimeOffset>());
		}
	}

	private readonly ConcurrentDictionary<string, ConcurrentBag<DateTimeOffset>> _executionFailuresMap = new ();

	private readonly IPriorityProvider _priorityProvider;
	private readonly IPriorityCalculator _priorityCalculator;
	private readonly IPersistence _persistence;
	private readonly TimeSpan _waitForLockTimeout;
	private readonly ILogger<PriorityManager> _logger;
}