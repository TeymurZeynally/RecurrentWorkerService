using System.Diagnostics;
using ExperimentalApplication.Payloads;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;

namespace ExperimentalApplication.Workers;

internal class CronWorker : ICronWorker
{
	private readonly IPayload _payload;
	private readonly ILogger<CronWorker> _logger;

    public CronWorker(IPayload payload, ILogger<CronWorker> logger)
    {
	    _payload = payload;
	    _logger = logger;
    }

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Cron worker");
		_logger.LogInformation($"Start");

		_logger.LogInformation($"Do work...");
		await _payload.ExecuteAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogInformation($"End");
	}
}