using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Configuration.Settings;
using RecurrentWorkerService.Distributed.Persistence;

namespace RecurrentWorkerService.Distributed.Services.Hosts;

internal class HeartbeatHostService : BackgroundService
{
	private readonly ILogger<HeartbeatHostService> _logger;
	private readonly IPersistence _persistence;
	private readonly HeartbeatSettings _settings;

	public HeartbeatHostService(ILogger<HeartbeatHostService> logger, IPersistence persistence, HeartbeatSettings settings)
	{
		_logger = logger;
		_persistence = persistence;
		_settings = settings;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogDebug("Heartbeat");
			await _persistence.HeartbeatAsync(_settings.HeartbeatExpirationTimeout, stoppingToken);
			await Task.Delay(_settings.HeartbeatPeriod, stoppingToken);
		}
	}
}