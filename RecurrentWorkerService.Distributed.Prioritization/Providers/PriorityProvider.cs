using System.Collections.Concurrent;

namespace RecurrentWorkerService.Distributed.Prioritization.Providers;

internal class PriorityProvider: IPriorityProvider
{
	public PriorityProvider(long nodeId)
	{
		_nodeId = nodeId;
	}

	public byte[] GetPrioritiesAsc(string identity)
	{
		return _prioritiesDictionary.TryGetValue(identity, out var priorities) ? priorities : Array.Empty<byte>();

	}
	public byte GetPriority(string identity)
	{
		return _currentNodeDictionary.TryGetValue(identity, out var priority) ? priority : byte.MinValue;
	}

	public void UpdatePriorityInformation((string Identity, long NodeId, byte Priority)[] priorities)
	{
		_prioritiesDictionary = new ConcurrentDictionary<string, byte[]>(
			priorities
				.GroupBy(x => x.Identity)
				.Select(x => new KeyValuePair<string, byte[]>(
					x.Key,
					x.Select(y => y.Priority).OrderBy(x => x).ToArray())));

		_currentNodeDictionary = new ConcurrentDictionary<string, byte>(
			priorities
				.Where(x => x.NodeId == _nodeId)
				.Select(x => new KeyValuePair<string, byte>(x.Identity, x.Priority)));
	}

	private IReadOnlyDictionary<string, byte[]> _prioritiesDictionary = new ConcurrentDictionary<string, byte[]>();
	private IReadOnlyDictionary<string, byte> _currentNodeDictionary = new ConcurrentDictionary<string, byte>();

	private readonly long _nodeId;
}