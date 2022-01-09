
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class WorkloadWorker : IWorkloadWorker
{
	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} WorkloadWorker");
		await Task.CompletedTask;

		return Workload.Full;
	}
}