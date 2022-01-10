namespace RecurrentWorkerService.Distributed.Services;

internal interface IDistributedWorkerService
{
	Task ExecuteAsync(CancellationToken stoppingToken);
}

