using RecurrentWorkerService.Distributed.Prioritization.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Providers
{
	internal interface IPriorityProvider
	{
		Priority[] GetPrioritiesAsc(string identity);

		Priority GetPriority(string identity);

		void UpdatePriorityInformation((string Identity, long NodeId, byte Priority)[] priorities);
	}
}
