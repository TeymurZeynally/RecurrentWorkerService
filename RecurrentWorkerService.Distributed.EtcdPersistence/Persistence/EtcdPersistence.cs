using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;
using V3Lockpb;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Persistence;

internal class EtcdPersistence: IPersistence
{
	public EtcdPersistence(ILogger<EtcdPersistence> logger, ChannelBase channel, string serviceId, long nodeId)
	{
		_logger = logger;
		_nodeId = nodeId;
		_serviceId = serviceId;
		_lockClient = new Lock.LockClient(channel);
		_leaseClient = new Lease.LeaseClient(channel);
		_kvClient = new KV.KVClient(channel);
		_watchClient = new Watch.WatchClient(channel);
	}
	public async Task<string?> AcquireExecutionLockAsync(string identity, CancellationToken cancellationToken)
	{
		if (!_serviceLeaseCreated)
		{
			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			return null;
		}

		var response = await _lockClient.LockAsync(
			new LockRequest { Lease = _nodeId, Name = ByteString.CopyFromUtf8(GetKeyForExecutionLock(identity)) },
			cancellationToken: cancellationToken);

		return response.Key.ToStringUtf8();
	}

	public async Task<PersistenceResponse> SucceededAsync(string identity, long scheduleIndex, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds },
			cancellationToken: cancellationToken);
		var response = await _kvClient.PutAsync(
			new PutRequest { Key = ByteString.CopyFromUtf8(GetKeyForSucceededIteration(identity, scheduleIndex)), Lease = lease.ID},
			cancellationToken: cancellationToken);

		return new() { Revision = response.Header.Revision };
	}

	public async Task<PersistenceResponse<bool>> IsSucceededAsync(string identity, long scheduleIndex, CancellationToken cancellationToken)
	{
		var result = await _kvClient.RangeAsync(
			new RangeRequest { CountOnly = true, Key = ByteString.CopyFromUtf8(GetKeyForSucceededIteration(identity, scheduleIndex)) },
			cancellationToken: cancellationToken);

		return new() { Data = result.Count > 0, Revision = result.Header.Revision };
	}

	public async Task HeartbeatAsync(TimeSpan expiration, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				using var keepAlive = _leaseClient.LeaseKeepAlive(cancellationToken: cancellationToken);
				await keepAlive.RequestStream.WriteAsync(new LeaseKeepAliveRequest { ID = _nodeId });
				await keepAlive.RequestStream.CompleteAsync();
				var anyResponse = await keepAlive.ResponseStream.MoveNext(cancellationToken);
				if (!anyResponse || keepAlive.ResponseStream.Current.TTL <= 0)
				{
					var ttlResponse = await _leaseClient.LeaseTimeToLiveAsync(new LeaseTimeToLiveRequest { ID = _nodeId }, cancellationToken: cancellationToken);

					if (ttlResponse == null || ttlResponse.TTL < 0)
					{
						await _leaseClient.LeaseGrantAsync(new LeaseGrantRequest { ID = _nodeId, TTL = (long)expiration.TotalSeconds }, cancellationToken: cancellationToken);
					}
				}

				_serviceLeaseCreated = true;
				return;
			}
			catch (RpcException e)
			{
				_logger.LogError($"Heartbeat error: {e}");
			}
		}
	}

	public async Task ReleaseExecutionLockAsync(string lockId, CancellationToken cancellationToken)
	{
		await _lockClient.UnlockAsync(
			new UnlockRequest { Key = ByteString.CopyFromUtf8(lockId) },
			cancellationToken: cancellationToken);
	}


	public async Task<PersistenceResponse<WorkloadInfo?>?> GetCurrentWorkloadAsync(string identity, CancellationToken cancellationToken)
	{
		var response = await _kvClient.RangeAsync(
			new RangeRequest { Key = ByteString.CopyFromUtf8(GetKeyForWorkload(identity)), Limit = 1 },
			cancellationToken: cancellationToken);

		if (response.Count == 0)
		{
			return null;
		}

		return new()
		{
			Data = JsonSerializer.Deserialize<WorkloadInfo>(response.Kvs.Single().Value.ToStringUtf8()),
			Revision = response.Header.Revision
		};
	}

	public async Task<PersistenceResponse> UpdateWorkloadAsync(string identity, WorkloadInfo workloadInfo, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds },
			cancellationToken: cancellationToken);

		var response = await _kvClient.PutAsync(
			new PutRequest
			{
				Key = ByteString.CopyFromUtf8(GetKeyForWorkload(identity)),
				Value = ByteString.CopyFromUtf8(JsonSerializer.Serialize(workloadInfo)),
				Lease = lease.ID
			},
			cancellationToken: cancellationToken);

		return new() { Revision = response.Header.Revision };
	}

	public async Task UpdatePriorityAsync(string identity, byte priority, CancellationToken cancellationToken)
	{
		await _kvClient.PutAsync(new PutRequest
			{
				Lease = _nodeId,
				Key = ByteString.CopyFromUtf8(GetKeyForIterationPriority(identity)),
				Value = ByteString.CopyFrom(priority),
			},
			cancellationToken: cancellationToken);
	}

	public async Task<(string Identity, long NodeId, byte Priority)[]> GetAllPrioritiesAsync(CancellationToken cancellationToken)
	{
		var iterationPrioritySearchKey = GetSearchKeyForIterationPriority();

		var response = await _kvClient.RangeAsync(
			new RangeRequest
			{
				Key = ByteString.CopyFromUtf8(iterationPrioritySearchKey),
				RangeEnd = ByteString.CopyFromUtf8(GetRangeEnd(iterationPrioritySearchKey))
			},
			cancellationToken: cancellationToken);

		return response.Kvs.Select(kv =>
		{
			var keyMatch = _priorityKeyRegex.Match(kv.Key.ToStringUtf8());
			var identity = keyMatch.Groups["identity"].Value;
			var node = Convert.ToInt64(keyMatch.Groups["nodeId"].Value);
			var priority = kv.Value.ToByteArray().Single();

			return (identity, node, priority);
		}).ToArray();
	}

	public async Task WaitForOrderAsync(int order, string identity, long revisionStart, CancellationToken cancellationToken)
	{
		if(order <= 0 || revisionStart <= 0) return;

		var key = GetKeyForExecutionLock(identity);

		using var watchStream = _watchClient.Watch(cancellationToken: cancellationToken);
		await watchStream.RequestStream.WriteAsync(
			new WatchRequest
			{
				CreateRequest = new WatchCreateRequest
				{
					Key = ByteString.CopyFromUtf8(key),
					RangeEnd = ByteString.CopyFromUtf8(GetRangeEnd(key)),
					StartRevision = revisionStart,
					Filters = { WatchCreateRequest.Types.FilterType.Nodelete }
				},
			});

		await watchStream.RequestStream.CompleteAsync();
		var count = default(int);
		while (await watchStream.ResponseStream.MoveNext(cancellationToken))
		{
			count += watchStream.ResponseStream.Current.Events.Count;
			if (count >= order)
			{
				break;
			}
		}
	}

	private string GetKeyForWorkload(string identity) => _serviceId + identity + "WorkLoad";

	private string GetKeyForExecutionLock(string identity) => _serviceId + identity + "Lock";

	private string GetKeyForSucceededIteration(string identity, long scheduleIndex) => _serviceId + identity + scheduleIndex + "Success";
	
	private string GetSearchKeyForIterationPriority() => _serviceId + "Priority";

	private string GetKeyForIterationPriority(string identity) => GetSearchKeyForIterationPriority() + identity + "["+ _nodeId + "]";

	private readonly Regex _priorityKeyRegex = new (@".+Priority(?<identity>.+)\[(?<nodeId>\d+)\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static string GetRangeEnd(string prefixKey)
	{
		if (prefixKey.Length == 0) return "\x00";
		var rangeEnd = new StringBuilder(prefixKey);
		rangeEnd[^1] = ++rangeEnd[^1];
		return rangeEnd.ToString();
	}


	private readonly ILogger<EtcdPersistence> _logger;
	private readonly long _nodeId;
	private readonly string _serviceId;
	private readonly Lock.LockClient _lockClient;
	private readonly Lease.LeaseClient _leaseClient;
	private readonly KV.KVClient _kvClient;
	private readonly Watch.WatchClient _watchClient;
	private bool _serviceLeaseCreated = false;
}
