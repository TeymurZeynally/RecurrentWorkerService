using RecurrentWorkerService.Workers;

namespace Application;

internal class ExampleOfCronWorker : ICronWorker
{
	private readonly ILogger<ExampleOfCronWorker> _logger;

	public ExampleOfCronWorker(ILogger<ExampleOfCronWorker> logger)
	{
		_logger = logger;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Cron worker");
		_logger.LogInformation("Start");

		_logger.LogInformation("Do something...");
		await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

		_logger.LogInformation("End");
	}
}