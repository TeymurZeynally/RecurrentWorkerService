namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public interface IPriorityManager
{
	Task WaitForExecutionOrderAsync(string identity, long revisionStart, TimeSpan lifetime, CancellationToken cancellationToken);

	Task ResetExecutionResultAsync(string identity, CancellationToken cancellationToken);

	Task DecreaseExecutionPriorityAsync(string identity, CancellationToken cancellationToken);
}