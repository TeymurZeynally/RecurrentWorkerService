namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal interface IPriorityChangesAggregator
{
	Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken);

	Task ResetPriorityAsync(string identity, CancellationToken cancellationToken);

	Task UpdateIdentityIndicatorPrioritiesAsync((string Identity, byte Measurement)[] indicators, CancellationToken cancellationToken);

	Task UpdateNodeIndicatorPrioritiesAsync(byte[] indicators, CancellationToken cancellationToken);
}