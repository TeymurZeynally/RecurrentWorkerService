using Microsoft.Extensions.Hosting;

namespace RecurrentWorkerService.Services.Hosts;

internal class WorkerHostedService : BackgroundService
{
	private readonly IEnumerable<IWorkerService> _services;

	public WorkerHostedService(IEnumerable<IWorkerService> services)
	{
		_services = services;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.WhenAll(_services.Select(x => x.ExecuteAsync(stoppingToken)));
	}
}