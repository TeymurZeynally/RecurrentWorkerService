using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal interface IComputedPriorityAggregator
{
	int GetNodeOrder(string identity);

	void UpdateIdentityPriorityInformation(PriorityEvent priorityEvent);

	void UpdateFailuresPriorityInformation(PriorityEvent priorityEvent);

	void UpdateNodePriorityInformation(NodePriorityEvent priorityEvent);

	void ResetPriorityInformation();

	void ResetNodePriorityInformation();
}