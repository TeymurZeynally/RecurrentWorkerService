using RecurrentWorkerService.Workers;

namespace Application;

internal class ExampleOfRecurrentWorker : IRecurrentWorker
{
	private readonly ILogger<ExampleOfRecurrentWorker> _logger;


    public ExampleOfRecurrentWorker(ILogger<ExampleOfRecurrentWorker> logger)
	{
		_logger = logger;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation($"Start");

		_logger.LogInformation($"Do something...");
		await Task.Delay(TimeSpan.FromSeconds(1));
	
		_logger.LogInformation($"End");
	}
}