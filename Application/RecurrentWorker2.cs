using RecurrentWorkerService.Workers;

namespace Application;

internal class RecurrentWorker2 : IRecurrentWorker
{
	public Task ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker2 Start");
		return Task.CompletedTask;
	}
}