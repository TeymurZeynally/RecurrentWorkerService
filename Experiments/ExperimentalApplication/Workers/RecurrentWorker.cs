using System.Diagnostics;
using ExperimentalApplication.Payloads;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;

namespace ExperimentalApplication.Workers;

internal class RecurrentWorker : IRecurrentWorker
{
	private readonly IPayload _payload;
	private readonly ILogger<RecurrentWorker> _logger;


    public RecurrentWorker(IPayload payload, ILogger<RecurrentWorker> logger)
    {
	    _payload = payload;
	    _logger = logger;
    }

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation($"Start");

		_logger.LogInformation($"Do work...");
		await _payload.ExecuteAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogInformation($"End");
	}
}