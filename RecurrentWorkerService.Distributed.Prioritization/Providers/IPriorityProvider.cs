using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Providers;

internal interface IPriorityProvider
{
	byte[] GetPrioritiesAsc(string identity);

	byte GetPriority(string identity);

	void UpdatePriorityInformation(PriorityEvent priorityEvent);

	void Reset();
}