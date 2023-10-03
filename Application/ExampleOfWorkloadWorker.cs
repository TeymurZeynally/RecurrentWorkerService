using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class ExampleOfWorkloadWorker : IWorkloadWorker
{
	private readonly ILogger<ExampleOfWorkloadWorker> _logger;
	private readonly Random _random;

	public ExampleOfWorkloadWorker(ILogger<ExampleOfWorkloadWorker> logger)
	{
		_logger = logger;
		_random = new Random();
	}

	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation("Start");

		_logger.LogInformation("Do something...");
		await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

		_logger.LogInformation("Calculate workload...");
		var workload = (Workload)_random.Next(Workload.Zero, Workload.Full);

		_logger.LogInformation("End");

		return workload;
	}
}