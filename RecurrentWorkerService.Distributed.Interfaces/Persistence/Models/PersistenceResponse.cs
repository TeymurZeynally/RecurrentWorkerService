namespace RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;

public class PersistenceResponse<T>
{
	public T Data { get; init; } = default!;

	public long Revision { get; init; }
}

public class PersistenceResponse
{
	public long Revision { get; init; }
}