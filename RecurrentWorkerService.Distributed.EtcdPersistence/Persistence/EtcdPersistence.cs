using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Mvccpb;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Persistence.Models;
using V3Lockpb;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Persistence;

internal class EtcdPersistence: IPersistence
{
	public EtcdPersistence(ILogger<EtcdPersistence> logger, ChannelBase channel, string serviceId, long nodeId, ActivitySource activitySource)
	{
		_logger = logger;
		_serviceId = serviceId;
		_nodeId = nodeId;
		_activitySource = activitySource;
		_activityTags = new[] { new KeyValuePair<string, object?>("node", nodeId) };
		_lockClient = new Lock.LockClient(channel);
		_leaseClient = new Lease.LeaseClient(channel);
		_kvClient = new KV.KVClient(channel);
		_watchClient = new Watch.WatchClient(channel);
	}
	public async Task<string?> AcquireExecutionLockAsync(string identity, CancellationToken cancellationToken)
	{
		await WaitForLeaseAsync().ConfigureAwait(false);

		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		var response = await _lockClient.LockAsync(
			new LockRequest { Lease = _nodeId, Name = ByteString.CopyFromUtf8(GetKeyForExecutionLock(identity)) },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return response.Key.ToStringUtf8();
	}

	public async Task<PersistenceResponse> SucceededAsync(string identity, long scheduleIndex, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds + 512 },
			cancellationToken: cancellationToken).ConfigureAwait(false);
		var response = await _kvClient.PutAsync(
			new PutRequest { Key = ByteString.CopyFromUtf8(GetKeyForSucceededIteration(identity, scheduleIndex)), Lease = lease.ID},
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return new() { Revision = response.Header.Revision };
	}

	public async Task<PersistenceResponse<bool>> IsSucceededAsync(string identity, long scheduleIndex, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		var result = await _kvClient.RangeAsync(
			new RangeRequest { CountOnly = true, Key = ByteString.CopyFromUtf8(GetKeyForSucceededIteration(identity, scheduleIndex)) },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return new() { Data = result.Count > 0, Revision = result.Header.Revision };
	}

	public async Task HeartbeatAsync(TimeSpan expiration, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				using var keepAlive = _leaseClient.LeaseKeepAlive(cancellationToken: cancellationToken);
				await keepAlive.RequestStream.WriteAsync(new LeaseKeepAliveRequest { ID = _nodeId }).ConfigureAwait(false);
				await keepAlive.RequestStream.CompleteAsync().ConfigureAwait(false);
				var anyResponse = await keepAlive.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
				if (!anyResponse || keepAlive.ResponseStream.Current.TTL <= 0)
				{
					var ttlResponse = await _leaseClient.LeaseTimeToLiveAsync(new LeaseTimeToLiveRequest { ID = _nodeId }, cancellationToken: cancellationToken).ConfigureAwait(false);

					if (ttlResponse == null || ttlResponse.TTL < 0)
					{
						await _leaseClient.LeaseGrantAsync(new LeaseGrantRequest { ID = _nodeId, TTL = (long)expiration.TotalSeconds }, cancellationToken: cancellationToken).ConfigureAwait(false);
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
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		await _lockClient.UnlockAsync(
			new UnlockRequest { Key = ByteString.CopyFromUtf8(lockId) },
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}


	public async Task<PersistenceResponse<WorkloadInfo?>?> GetCurrentWorkloadAsync(string identity, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		var response = await _kvClient.RangeAsync(
			new RangeRequest { Key = ByteString.CopyFromUtf8(GetKeyForWorkload(identity)), Limit = 1 },
			cancellationToken: cancellationToken).ConfigureAwait(false);

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
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		var response = await _kvClient.PutAsync(
			new PutRequest
			{
				Key = ByteString.CopyFromUtf8(GetKeyForWorkload(identity)),
				Value = ByteString.CopyFromUtf8(JsonSerializer.Serialize(workloadInfo)),
				Lease = lease.ID
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return new() { Revision = response.Header.Revision };
	}

	public async Task UpdatePriorityAsync(string identity, byte priority, CancellationToken cancellationToken)
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		await WaitForLeaseAsync().ConfigureAwait(false);
		await _kvClient.PutAsync(new PutRequest
			{
				Lease = _nodeId,
				Key = ByteString.CopyFromUtf8(GetKeyForIterationPriority(identity)),
				Value = ByteString.CopyFrom(priority),
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}

	public async Task UpdateNodePriorityAsync(byte priority, CancellationToken cancellationToken)
	{
		await WaitForLeaseAsync().ConfigureAwait(false);

		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		await _kvClient.PutAsync(new PutRequest
			{
				Lease = _nodeId,
				Key = ByteString.CopyFromUtf8(GetKeyForNodePriority()),
				Value = ByteString.CopyFrom(priority),
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<PriorityEvent> WatchPriorityUpdates(CancellationToken cancellationToken)
	{
		PriorityEvent ConvertToPriorityEvent(KeyValue kv)
		{
			var keyParts = kv.Key.ToStringUtf8().Split("/");
			var identity = keyParts[2];
			var node = Convert.ToInt64(keyParts[3]);
			var priority = kv.Value.IsEmpty ? default(byte?) : kv.Value.ToByteArray().Single();

			return new PriorityEvent { Revision = kv.ModRevision, Identity = identity, NodeId = node, Priority = priority };
		};

		return WatchUpdates(GetSearchKeyForIterationPriority(), ConvertToPriorityEvent, cancellationToken);
	}

	public IAsyncEnumerable<NodePriorityEvent> WatchNodePriorityUpdates(CancellationToken cancellationToken)
	{
		NodePriorityEvent ConvertToPriorityEvent(KeyValue kv)
		{
			var keyParts = kv.Key.ToStringUtf8().Split("/");
			var node = Convert.ToInt64(keyParts[2]);
			var priority = kv.Value.IsEmpty ? default(byte?) : kv.Value.ToByteArray().Single();

			return new NodePriorityEvent { Revision = kv.ModRevision, NodeId = node, Priority = priority };
		};

		return WatchUpdates(GetSearchKeyForNodePriority(), ConvertToPriorityEvent, cancellationToken);
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
			}).ConfigureAwait(false);

		await watchStream.RequestStream.CompleteAsync().ConfigureAwait(false);
		var count = default(int);
		while (await watchStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
		{
			count += watchStream.ResponseStream.Current.Events.Count;
			if (count >= order)
			{
				break;
			}
		}
	}

	private async IAsyncEnumerable<T> WatchUpdates<T>(string searchKey, Func<KeyValue, T> convertFunc, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var key = ByteString.CopyFromUtf8(searchKey);
		var rangeEnd = ByteString.CopyFromUtf8(GetRangeEnd(searchKey));

		var range = await _kvClient.RangeAsync(new RangeRequest { Key = key, RangeEnd = rangeEnd }, cancellationToken: cancellationToken).ConfigureAwait(false);

		foreach (var kv in range.Kvs)
		{
			yield return convertFunc(kv);
		}

		using var watchStream = _watchClient.Watch(cancellationToken: cancellationToken);
		await watchStream.RequestStream.WriteAsync(
			new WatchRequest
			{
				CreateRequest = new WatchCreateRequest { Key = key, RangeEnd = rangeEnd, StartRevision = range.Header.Revision + 1 },
			}).ConfigureAwait(false);
		await watchStream.RequestStream.CompleteAsync().ConfigureAwait(false);

		while (await watchStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
		{
			foreach (var @event in watchStream.ResponseStream.Current.Events)
			{
				yield return convertFunc(@event.Kv);
			}
		}
	}

	private async Task WaitForLeaseAsync()
	{
		using var activity = _activitySource.StartActivity(ActivityKind.Internal, tags: _activityTags);

		for (int count = 0; count < 3; count++)
		{
			if (_serviceLeaseCreated)
			{
				return;
			}

			await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
		}

		throw new TimeoutException("No lease for 3 seconds");
	}

	private string GetKeyForWorkload(string identity) => $"{_serviceId}/{identity}/WorkLoad";

	private string GetKeyForExecutionLock(string identity) => $"{_serviceId}/{identity}/Lock";

	private string GetKeyForSucceededIteration(string identity, long scheduleIndex) => $"{_serviceId}/{identity}/{scheduleIndex}/Success";
	
	private string GetSearchKeyForIterationPriority() => $"{_serviceId}/Priority";

	private string GetKeyForIterationPriority(string identity) => $"{GetSearchKeyForIterationPriority()}/{identity}/{_nodeId}";

	private string GetSearchKeyForNodePriority() => $"{_serviceId}/NodePriority";

	private string GetKeyForNodePriority() => $"{GetSearchKeyForNodePriority()}/{_nodeId}";


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
	private readonly ActivitySource _activitySource;
	private readonly KeyValuePair<string, object?>[] _activityTags;

	private readonly Lock.LockClient _lockClient;
	private readonly Lease.LeaseClient _leaseClient;
	private readonly KV.KVClient _kvClient;
	private readonly Watch.WatchClient _watchClient;
	private bool _serviceLeaseCreated = false;
}
