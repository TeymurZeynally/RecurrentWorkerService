using System.Diagnostics;

namespace ExperimentalApplication.Payloads
{
	internal class ImmediatePayload : IPayload
	{
		private readonly ActivitySource _activitySource;
		private readonly long _nodeId;
		private readonly string _identity;

		public ImmediatePayload(ActivitySource activitySource, long nodeId, string identity)
		{
			_activitySource = activitySource;
			_nodeId = nodeId;
			_identity = identity;
		}

		public async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			using var activity = _activitySource.StartActivity(name: $"{nameof(ImmediatePayload)}.{nameof(ExecuteAsync)}");
			activity?.AddTag("node", _nodeId);
			activity?.AddTag("identity", _identity);
			await Task.CompletedTask;
		}
	}
}
