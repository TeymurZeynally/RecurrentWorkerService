using dotnet_etcd;
using RecurrentWorkerService.Distributed.Persistence;
using StackExchange.Redis;

namespace RecurrentWorkerService.Distributed.RedisPersistence.Persistence;

internal class RedisPersistence : IPersistence
{
	private readonly IDatabase _database;
	private readonly string _nodeId;
	private readonly string _serviceId;


	public RedisPersistence(IDatabase database, string serviceId, string nodeId)
	{
		_database = database;
		_serviceId = serviceId;
		_nodeId = nodeId;
	}

	public async Task<string?> AcquireExecutionLockAsync(string identity, long scheduleIndex, int iterationIndex)
	{
		var lockId = $"{_serviceId}-{identity}-{scheduleIndex}-{iterationIndex}-LOCK";

		if (await IsSucceededAsync(identity, scheduleIndex, iterationIndex)) return null;
		var takenLockNode = await _database.LockQueryAsync(lockId);
		if (takenLockNode != RedisValue.Null)
		{
			if (await _database.StringGetAsync($"{_serviceId}-{takenLockNode}-NODE") == RedisValue.Null)
			{
				await _database.LockReleaseAsync(lockId, takenLockNode);
			}
		}

		return await _database.LockTakeAsync(lockId, _nodeId, TimeSpan.MaxValue) ? lockId : null;
	}

	public async Task HeartbeatAsync(TimeSpan expiration)
	{
		await _database.StringSetAsync($"{_serviceId}-{_nodeId}-NODE", "ALIVE", expiration);
	}

	public async Task ReleaseExecutionLockAsync(string lockId)
	{
		await _database.LockReleaseAsync(lockId, _nodeId);
	}

	public async Task SucceededAsync(string identity, long scheduleIndex, int iterationIndex)
	{
		await _database.SetAddAsync($"{_serviceId}-{identity}-{scheduleIndex}-SUCCEDED", iterationIndex);
		await _database.KeyExpireAsync($"{_serviceId}-{identity}-{scheduleIndex}-SUCCEDED", TimeSpan.FromSeconds(10));
	}

	public async Task<bool> IsSucceededAsync(string identity, long scheduleIndex, int iterationIndex)
	{
		return await _database.SetContainsAsync($"{_serviceId}-{identity}-{scheduleIndex}-SUCCEDED", iterationIndex);
	}
}