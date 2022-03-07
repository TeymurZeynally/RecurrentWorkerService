using RecurrentWorkerService.Distributed.Prioritization.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal interface  IPriorityCalculator
{
	Task<Priority> GetPriorityAsync(DateTimeOffset[] failuresHistory, CancellationToken cancellationToken);
}