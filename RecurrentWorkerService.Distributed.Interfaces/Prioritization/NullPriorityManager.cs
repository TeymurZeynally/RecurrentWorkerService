namespace RecurrentWorkerService.Distributed.Interfaces.Prioritization;

public class NullPriorityManager : IPriorityManager
{
	public Task WaitForExecutionOrderAsync(string identity, long revisionStart, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task ResetPriorityAsync(string identity, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task DecreasePriorityAsync(string identity, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}