using System.Collections.Concurrent;
using RecurrentWorkerService.Distributed.Prioritization.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Providers
{
	internal class PriorityProvider: IPriorityProvider
	{
		public PriorityProvider(long nodeId)
		{
			_nodeId = nodeId;
		}

		public Priority[] GetPrioritiesAsc(string identity)
		{
			return _prioritiesDictionary.TryGetValue(identity, out var priorities) ? priorities : Array.Empty<Priority>();

		}
		public Priority GetPriority(string identity)
		{
			return _currentNodeDictionary.TryGetValue(identity, out var priority) ? priority : Priority.High;
		}

		public void UpdatePriorityInformation((string Identity, long NodeId, byte Priority)[] priorities)
		{
			_prioritiesDictionary = new ConcurrentDictionary<string, Priority[]>(
				priorities
					.GroupBy(x => x.Identity)
					.Select(x => new KeyValuePair<string, Priority[]>(
						x.Key,
						x.Select(y => (Priority)y.Priority).ToArray())));

			_currentNodeDictionary = new ConcurrentDictionary<string, Priority>(
				priorities
					.Where(x => x.NodeId == _nodeId)
					.Select(x => new KeyValuePair<string, Priority>(x.Identity, x.Priority)));
		}

		private IReadOnlyDictionary<string, Priority[]> _prioritiesDictionary = new ConcurrentDictionary<string, Priority[]>();
		private IReadOnlyDictionary<string, Priority> _currentNodeDictionary = new ConcurrentDictionary<string, Priority>();

		private readonly long _nodeId;
	}
}
