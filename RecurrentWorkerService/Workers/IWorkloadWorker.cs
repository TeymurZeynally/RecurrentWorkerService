using RecurrentWorkerService.Workers.Models;

namespace RecurrentWorkerService.Workers;

public interface IWorkloadWorker
{
	Task<Workload> ExecuteAsync(CancellationToken cancellationToken);
}