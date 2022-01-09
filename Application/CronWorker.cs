using RecurrentWorkerService.Workers;

namespace Application;

internal class CronWorker : ICronWorker
{
	public Task ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} CronWorker");
		return Task.CompletedTask;
	}
}