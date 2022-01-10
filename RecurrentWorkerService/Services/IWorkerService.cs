namespace RecurrentWorkerService.Services;

internal interface IWorkerService
{
	Task ExecuteAsync(CancellationToken stoppingToken);
}

