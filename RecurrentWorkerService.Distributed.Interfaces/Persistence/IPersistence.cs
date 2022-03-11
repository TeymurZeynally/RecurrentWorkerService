using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

namespace RecurrentWorkerService.Distributed.Interfaces.Persistence;

public interface IPersistence
{
	Task HeartbeatAsync(TimeSpan expiration, CancellationToken cancellationToken);

	Task<string?> AcquireExecutionLockAsync(string identity, CancellationToken cancellationToken);

	Task ReleaseExecutionLockAsync(string lockId, CancellationToken cancellationToken);

	Task<PersistenceResponse> SucceededAsync(string identity, long scheduleIndex, TimeSpan lifetime, CancellationToken cancellationToken);

	Task<PersistenceResponse<bool>> IsSucceededAsync(string identity, long scheduleIndex, CancellationToken cancellationToken);

	Task<PersistenceResponse<WorkloadInfo?>?> GetCurrentWorkloadAsync(string identity, CancellationToken cancellationToken);

	Task<PersistenceResponse> UpdateWorkloadAsync(string identity, WorkloadInfo workloadInfo, TimeSpan lifetime, CancellationToken cancellationToken);

	Task UpdatePriorityAsync(string identity, byte priority, CancellationToken cancellationToken);

	IAsyncEnumerable<PriorityEvent> WatchPriorityUpdates(CancellationToken cancellationToken);

	Task WaitForOrderAsync(int order, string identity, long revisionStart, CancellationToken cancellationToken);
}