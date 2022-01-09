namespace RecurrentWorkerService.Distributed.Services.Settings
{
	internal class HeartbeatSettings
	{
		public TimeSpan HeartbeatPeriod { get; init; }

		public TimeSpan HeartbeatExpirationTimeout { get; init; }
	}
}
