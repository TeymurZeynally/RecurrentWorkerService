namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public interface IPriorityManager
{
	Task WaitForExecutionOrderAsync(string identity, long revisionStart, TimeSpan lifetime, CancellationToken cancellationToken);

	Task ResetPriorityAsync(string identity, CancellationToken cancellationToken);

	Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken);
}