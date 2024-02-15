namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public class NullPriorityManager : IPriorityManager
{
	public Task WaitForExecutionOrderAsync(string identity, long revisionStart, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public bool IsFirstInExecutionOrder(string identity, TimeSpan waitTime)
	{
		return true;
	}

	public Task ResetExecutionResultAsync(string identity, bool force, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task DecreaseExecutionPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}