using Microsoft.Extensions.Hosting;

namespace RecurrentWorkerService.Distributed.Services.Hosts;

internal class DistributedWorkerHostedService : BackgroundService
{
	private readonly IEnumerable<IDistributedWorkerService> _services;

	public DistributedWorkerHostedService(IEnumerable<IDistributedWorkerService> services)
	{
		_services = services;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.WhenAll(_services.Select(x => x.ExecuteAsync(stoppingToken))).ConfigureAwait(false);
	}
}