using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal interface IComputedPriorityAggregator
{
	int GetNodeOrder(string identity);

	void UpdatePriorityInformation(PriorityEvent priorityEvent);

	void ResetPriorityInformation();

	void UpdateNodePriorityInformation(NodePriorityEvent priorityEvent);

	void ResetNodePriorityInformation();
}