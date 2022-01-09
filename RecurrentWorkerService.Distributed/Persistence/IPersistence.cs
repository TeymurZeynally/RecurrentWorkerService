namespace RecurrentWorkerService.Distributed.Persistence;

internal interface IPersistence
{
	Task HeartbeatAsync(TimeSpan expiration);

	Task<string?> AcquireExecutionLockAsync(string identity, long scheduleIndex, int iterationIndex);

	Task ReleaseExecutionLockAsync(string lockId);

	Task SucceededAsync(string identity, long scheduleIndex, int iterationIndex);

	Task<bool> IsSucceededAsync(string identity, long scheduleIndex, int iterationIndex);
}