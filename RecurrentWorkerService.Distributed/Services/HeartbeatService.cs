using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Services.Settings;

namespace RecurrentWorkerService.Distributed.Services;

internal class HeartbeatService : BackgroundService
{
	private readonly ILogger<HeartbeatService> _logger;
	private readonly IPersistence _persistence;
	private readonly HeartbeatSettings _settings;

	public HeartbeatService(ILogger<HeartbeatService> logger, IPersistence persistence, HeartbeatSettings settings)
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
			await _persistence.HeartbeatAsync(_settings.HeartbeatExpirationTimeout);
			await Task.Delay(_settings.HeartbeatPeriod, stoppingToken);
		}
	}
}