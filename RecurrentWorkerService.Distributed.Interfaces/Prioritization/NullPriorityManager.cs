namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public class NullPriorityManager : IPriorityManager
{
	public Task WaitForExecutionOrderAsync(string identity, long revisionStart, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task ResetExecutionResultAsync(string identity, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task DecreaseExecutionPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}