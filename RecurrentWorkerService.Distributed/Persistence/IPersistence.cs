namespace RecurrentWorkerService.Distributed.Persistence;

public interface IPersistence
{
	Task HeartbeatAsync(TimeSpan expiration, CancellationToken cancellationToken);

	Task<string?> AcquireExecutionLockAsync(string identity, CancellationToken cancellationToken);

	Task ReleaseExecutionLockAsync(string lockId, CancellationToken cancellationToken);

	Task SucceededAsync(string identity, long scheduleIndex, TimeSpan lifetime, CancellationToken cancellationToken);

	Task<bool> IsSucceededAsync(string identity, long scheduleIndex, CancellationToken cancellationToken);
}