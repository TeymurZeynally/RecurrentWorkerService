namespace RecurrentWorkerService.Distributed.Prioritization.Aggregators;

internal interface IPriorityChangesAggregator
{
	Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken);

	Task ResetPriorityAsync(string identity, CancellationToken cancellationToken);

	Task UpdateIndicatorPriorities(byte[] indicators, CancellationToken cancellationToken);
}