
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class WorkloadWorker : IWorkloadWorker
{
	private readonly ILogger<WorkloadWorker> _logger
		;

	public WorkloadWorker(ILogger<WorkloadWorker> logger)
	{
		_logger = logger;
	}


	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation($"{DateTimeOffset.UtcNow} WorkloadWorker Start");

		await Task.Delay(TimeSpan.FromSeconds(2));
		throw new Exception("KEK");

		_logger.LogInformation($"{DateTimeOffset.UtcNow} WorkloadWorker End");

		return Workload.Percent(100);
	}
}