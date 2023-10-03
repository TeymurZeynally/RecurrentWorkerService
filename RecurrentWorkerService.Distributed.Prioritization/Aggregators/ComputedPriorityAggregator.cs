using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal class ComputedPriorityAggregator: IComputedPriorityAggregator
{
	public ComputedPriorityAggregator(long nodeId, ActivitySource activitySource, ILogger<ComputedPriorityAggregator> logger)
	{
		_nodeId = nodeId;
		_activitySource = activitySource;
		_activityNodeTags = new[] { new KeyValuePair<string, object?>("node", nodeId) };
		_logger = logger;
	}

	public int GetNodeOrder(string identity)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		if (!_prioritiesDictionary.TryGetValue(identity, out var nodesDict))
		{
			return 0;
		}

		var nodesPriorityOrder = nodesDict
			.Select(x => (IdPriority: x.Value, NodePriority: _nodePrioritiesDictionary.GetValueOrDefault(x.Key)))
			.OrderBy(x => x.IdPriority).ThenBy(x => x.NodePriority)
			.ToArray();

		nodesDict.TryGetValue(_nodeId, out var idPriority);
		_nodePrioritiesDictionary.TryGetValue(_nodeId, out var nodePriority);

		var order = Math.Max(0, Array.IndexOf(nodesPriorityOrder, (IdPriority: idPriority, NodePriority: nodePriority)));

		var priorities = string.Join(' ', nodesPriorityOrder);
		var priority = (IdPriority: idPriority, NodePriority: nodePriority);
		_logger.LogDebug("Priorities: [{Priorities}] Priority: [{Priority}] Order: {Order}", string.Join(' ', nodesPriorityOrder), (IdPriority: idPriority, NodePriority: nodePriority), order);
		activity?.AddTag("priorities", priorities);
		activity?.AddTag("priority", priority);
		activity?.AddTag("order", order);
		activity?.AddTag("identity", identity);

		return order;
	}

	public void UpdatePriorityInformation(PriorityEvent priorityEvent)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);
		activity?.AddTag("priority_event_id", priorityEvent.Revision);

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
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		_prioritiesDictionary = new();
	}

	public void UpdateNodePriorityInformation(NodePriorityEvent priorityEvent)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);
		activity?.AddTag("priority_event_id", priorityEvent.Revision);

		if (priorityEvent.Priority.HasValue)
		{
			_nodePrioritiesDictionary.AddOrUpdate(priorityEvent.NodeId, priorityEvent.Priority.Value, (_, _) => priorityEvent.Priority.Value);
		}
		else
		{
			_nodePrioritiesDictionary.TryRemove(priorityEvent.NodeId, out _);
		}
	}

	public void ResetNodePriorityInformation()
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityNodeTags);

		_nodePrioritiesDictionary = new();
	}

	private ConcurrentDictionary <string, ConcurrentDictionary<long, byte>> _prioritiesDictionary = new ();
	private ConcurrentDictionary<long, byte> _nodePrioritiesDictionary = new();

	private readonly long _nodeId;
	private readonly ActivitySource _activitySource;
	private readonly ILogger<ComputedPriorityAggregator> _logger;

	private readonly KeyValuePair<string, object?>[] _activityNodeTags;
}