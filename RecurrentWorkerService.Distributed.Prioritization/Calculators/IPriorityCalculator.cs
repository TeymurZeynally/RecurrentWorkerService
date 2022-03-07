namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal interface  IPriorityCalculator
{
	Task<byte> GetPriorityAsync(DateTimeOffset[] failuresHistory, CancellationToken cancellationToken);
}