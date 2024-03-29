﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Configuration.Settings;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;

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
			_logger.LogTrace("Heartbeat");
			await _persistence.HeartbeatAsync(_settings.HeartbeatExpirationTimeout, stoppingToken).ConfigureAwait(false);
			await Task.Delay(_settings.HeartbeatPeriod, stoppingToken).ConfigureAwait(false);
		}
	}
}