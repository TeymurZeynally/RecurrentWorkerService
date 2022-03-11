using System.Collections.Concurrent;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Providers;

internal class PriorityProvider: IPriorityProvider
{
	public PriorityProvider(long nodeId)
	{
		_nodeId = nodeId;
	}

	public byte[] GetPrioritiesAsc(string identity)
	{
		return _prioritiesDictionary.TryGetValue(identity, out var nodesDict) 
			? nodesDict.Values.OrderBy(x => x).ToArray()
			: Array.Empty<byte>();
	}

	public byte GetPriority(string identity)
	{
		if (_prioritiesDictionary.TryGetValue(identity, out var nodesDict) && nodesDict.TryGetValue(_nodeId, out var priority))
		{
			return priority;
		}

		return byte.MinValue;
	}

	public void UpdatePriorityInformation(PriorityEvent priorityEvent)
	{
		_prioritiesDictionary.TryAdd(priorityEvent.Identity, new ConcurrentDictionary<long, byte>());

		if(priorityEvent.Priority.HasValue)
		{
			var priority = priorityEvent.Priority.Value;
			_prioritiesDictionary[priorityEvent.Identity].AddOrUpdate(priorityEvent.NodeId, priority, (_, _) =>priority);
		}
		else
		{
			_prioritiesDictionary[priorityEvent.Identity].TryRemove(priorityEvent.NodeId, out _);
		}
	}

	public void Reset()
	{
		_prioritiesDictionary = new();
	}


	private ConcurrentDictionary <string, ConcurrentDictionary<long, byte>> _prioritiesDictionary = new ();

	private readonly long _nodeId;
}