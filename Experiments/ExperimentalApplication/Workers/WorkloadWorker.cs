using System.Diagnostics;
using ExperimentalApplication.Payloads;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace ExperimentalApplication.Workers;

internal class WorkloadWorker : IWorkloadWorker
{
	private readonly IPayload _payload;
	private readonly string _payloadName;
	private readonly ActivitySource _activitySource;
	private readonly ILogger<WorkloadWorker> _logger;
	private readonly Random _random;

	public WorkloadWorker(IPayload payload, string payloadName, ActivitySource activitySource, ILogger<WorkloadWorker> logger)
	{
		_payload = payload;
		_payloadName = payloadName;
		_activitySource = activitySource;
		_logger = logger;
		_random = new Random();
	}

	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(name: $"{nameof(WorkloadWorker)}.{nameof(ExecuteAsync)}-{_payloadName}");

		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation($"Start");

		_logger.LogInformation($"Do work...");
		await _payload.ExecuteAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogInformation($"Calculate workload...");
		var workload = (Workload)_random.Next(Workload.Zero, Workload.Full);

		_logger.LogInformation($"End");

		activity?.AddTag("workload", workload.Value);

		return workload;
	}
}