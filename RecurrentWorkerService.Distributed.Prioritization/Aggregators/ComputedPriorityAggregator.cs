using System.Collections.Concurrent;
using System.Diagnostics;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal class ComputedPriorityAggregator: IComputedPriorityAggregator
{
	public ComputedPriorityAggregator(long nodeId)
	{
		_nodeId = nodeId;
	}

	public int GetNodeOrder(string identity)
	{
		if (!_prioritiesDictionary.TryGetValue(identity, out var nodesDict))
		{
			return 0;
		}

		var sw = new Stopwatch();
		sw.Start();

		var nodesPriorityOrder = nodesDict
			.Select(x => (IdPriority: x.Value, NodePriority: _nodePrioritiesDictionary.GetValueOrDefault(x.Key)))
			.OrderBy(x => x.IdPriority).ThenBy(x => x.NodePriority)
			.ToArray();

		nodesDict.TryGetValue(_nodeId, out var idPriority);
		_nodePrioritiesDictionary.TryGetValue(_nodeId, out var nodePriority);

		var order = Math.Max(0, Array.IndexOf(nodesPriorityOrder, (IdPriority: idPriority, NodePriority: nodePriority)));

		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine("[||||||||||||||||||||||||||||||||||]");
		Console.WriteLine($"ELAPSED: {sw.Elapsed}; PRIORITIES: [{string.Join(' ', nodesPriorityOrder)}]  PRIORITY: {(IdPriority: idPriority, NodePriority: nodePriority)} ORDER: {order}");
		Console.WriteLine("[||||||||||||||||||||||||||||||||||]");
		Console.ResetColor();

		return order;
	}

	public void UpdatePriorityInformation(PriorityEvent priorityEvent)
	{
		_prioritiesDictionary.TryAdd(priorityEvent.Identity, new ConcurrentDictionary<long, byte>());

		if (priorityEvent.Priority.HasValue)
		{
			var priority = priorityEvent.Priority.Value;
			_prioritiesDictionary[priorityEvent.Identity].AddOrUpdate(priorityEvent.NodeId, priority, (_, _) => priority);
		}
		else
		{
			_prioritiesDictionary[priorityEvent.Identity].TryRemove(priorityEvent.NodeId, out _);
		}
	}

	public void ResetPriorityInformation()
	{
		_prioritiesDictionary = new();
	}

	public void UpdateNodePriorityInformation(long nodeId, byte? priority)
	{
		if (priority.HasValue)
		{
			_nodePrioritiesDictionary.AddOrUpdate(nodeId, priority.Value, (_, _) => priority.Value);
		}
		else
		{
			_nodePrioritiesDictionary.TryRemove(nodeId, out _);
		}
	}

	public void ResetNodePriorityInformation()
	{
		_nodePrioritiesDictionary = new();
	}

	private ConcurrentDictionary <string, ConcurrentDictionary<long, byte>> _prioritiesDictionary = new ();
	private ConcurrentDictionary<long, byte> _nodePrioritiesDictionary = new();

	private readonly long _nodeId;
}