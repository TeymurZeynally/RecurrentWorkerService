namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal interface  IPriorityCalculator
{
	Task<byte> GetPriorityAsync(DateTimeOffset[] failuresCount, CancellationToken cancellationToken);

	Task<byte> GetNodePriorityAsync(byte[][] indicators, CancellationToken cancellationToken);
}