using RecurrentWorkerService.Workers;

namespace Application;

internal class CronWorker : ICronWorker
{
	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} CronWorker");

		await Task.Delay(TimeSpan.FromSeconds(0));
		throw new Exception("KEEK");

		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker End");
	}
}