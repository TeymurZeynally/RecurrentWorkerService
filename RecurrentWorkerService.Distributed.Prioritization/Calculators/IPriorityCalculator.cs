namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal interface  IPriorityCalculator
{
	Task<byte> GetFailuresPriorityAsync(DateTimeOffset[] failuresCount, CancellationToken cancellationToken);

	Task<byte> GetIdentityPriorityAsync(byte[] indicators, CancellationToken cancellationToken);

	Task<byte> GetNodePriorityAsync(byte[] indicators, CancellationToken cancellationToken);
}