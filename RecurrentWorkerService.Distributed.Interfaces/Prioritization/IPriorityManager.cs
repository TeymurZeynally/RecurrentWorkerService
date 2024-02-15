namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public interface IPriorityManager
{
	Task WaitForExecutionOrderAsync(string identity, long revisionStart, CancellationToken cancellationToken);

	bool IsFirstInExecutionOrder(string identity, TimeSpan waitTime);

	Task ResetExecutionResultAsync(string identity, bool force, CancellationToken cancellationToken);

	Task DecreaseExecutionPriorityAsync(string identity, CancellationToken cancellationToken);
}