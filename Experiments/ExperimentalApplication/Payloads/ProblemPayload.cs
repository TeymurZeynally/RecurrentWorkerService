using System.Diagnostics;

namespace ExperimentalApplication.Payloads;

internal class ProblemPayload : IPayload
{
	public ProblemPayload(int failProbabilityPercent, ActivitySource activitySource, long nodeId, string identity)
	{
		_failProbabilityPercent = failProbabilityPercent;
		_activitySource = activitySource;
		_nodeId = nodeId;
		_identity = identity;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(name: $"{nameof(ProblemPayload)}.{nameof(ExecuteAsync)}");
		activity?.AddTag("node", _nodeId);
		activity?.AddTag("identity", _identity);

		var random = new Random();
		await Task.Delay(TimeSpan.FromSeconds(random.Next(1, 8)), cancellationToken).ConfigureAwait(false);

		if (new Random().Next(0, 100) < _failProbabilityPercent)
		{
			throw new Exception("Payload Failed!");
		}
	}

	private readonly int _failProbabilityPercent;
	private readonly ActivitySource _activitySource;
	private readonly long _nodeId;
	private readonly string _identity;
}