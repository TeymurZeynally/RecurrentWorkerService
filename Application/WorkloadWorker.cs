
using Application.Helpers;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class WorkloadWorker : IWorkloadWorker
{
	private readonly ILogger<WorkloadWorker> _logger;

	public WorkloadWorker(ILogger<WorkloadWorker> logger)
	{
		_logger = logger;
	}


	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation($"{DateTimeOffset.UtcNow} WorkloadWorker Start");

		if (!FailHelper.IsFail())
		{
			await Task.CompletedTask;
			throw new Exception("FAIL");
		}
		Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
		Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
		Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
		Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
		Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");


		_logger.LogInformation($"{DateTimeOffset.UtcNow} WorkloadWorker End");

		return Workload.Percent(100);
	}
}