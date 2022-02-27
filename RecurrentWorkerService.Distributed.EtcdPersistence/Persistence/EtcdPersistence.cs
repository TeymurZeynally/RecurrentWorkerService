using System.Text.Json;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Persistence.Models;
using V3Lockpb;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Persistence;

internal class EtcdPersistence: IPersistence
{
	private readonly ILogger<EtcdPersistence> _logger;
	private readonly long _nodeId;
	private readonly string _serviceId;
	private readonly Lock.LockClient _lockClient;
	private readonly Lease.LeaseClient _leaseClient;
	private readonly KV.KVClient _kvClient;
	private bool _leaseCreated = false;

	public EtcdPersistence(ILogger<EtcdPersistence> logger, ChannelBase channel, string serviceId, long nodeId)
	{
		_logger = logger;
		_nodeId = nodeId;
		_serviceId = serviceId;
		_lockClient = new Lock.LockClient(channel);
		_leaseClient = new Lease.LeaseClient(channel);
		_kvClient = new KV.KVClient(channel);
	}
	public async Task<string?> AcquireExecutionLockAsync(string identity, CancellationToken cancellationToken)
	{
		if (!_leaseCreated)
		{
			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			return null;
		}

		var response = await _lockClient.LockAsync(
			new LockRequest { Lease = _nodeId, Name = ByteString.CopyFromUtf8(_serviceId + identity) },
			cancellationToken: cancellationToken);

		return response.Key.ToStringUtf8();
	}

	public async Task SucceededAsync(string identity, long scheduleIndex, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds },
			cancellationToken: cancellationToken);
		await _kvClient.PutAsync(
			new PutRequest { Key = ByteString.CopyFromUtf8(_serviceId + identity + scheduleIndex), Lease = lease.ID},
			cancellationToken: cancellationToken);
	}

	public async Task<bool> IsSucceededAsync(string identity, long scheduleIndex, CancellationToken cancellationToken)
	{
		var result = await _kvClient.RangeAsync(
			new RangeRequest { CountOnly = true, Key = ByteString.CopyFromUtf8(_serviceId + identity + scheduleIndex) },
			cancellationToken: cancellationToken);

		return result.Count > 0;
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
					await GrantLease(expiration, cancellationToken);
				}

				_leaseCreated = true;
				return;
			}
			catch (RpcException e)
			{
				_logger.LogError($"Heartbeat error: {e}");
			}
		}
	}

	private async Task GrantLease(TimeSpan expiration, CancellationToken cancellationToken)
	{
		var ttlResponse = await _leaseClient.LeaseTimeToLiveAsync(
			new LeaseTimeToLiveRequest { ID = _nodeId },
			cancellationToken: cancellationToken);

		if (ttlResponse == null || ttlResponse.TTL < 0)
		{
			await _leaseClient.LeaseGrantAsync(
				new LeaseGrantRequest { ID = _nodeId, TTL = (long)expiration.TotalSeconds },
			cancellationToken: cancellationToken);
		}
	}

	public async Task ReleaseExecutionLockAsync(string lockId, CancellationToken cancellationToken)
	{
		await _lockClient.UnlockAsync(
			new UnlockRequest { Key = ByteString.CopyFromUtf8(lockId) },
			cancellationToken: cancellationToken);
	}


	public async Task<WorkloadInfo?> GetCurrentWorkloadAsync(string identity, CancellationToken cancellationToken)
	{
		var response = await _kvClient.RangeAsync(
			new RangeRequest { Key = ByteString.CopyFromUtf8(_serviceId + identity + "WL") },
			cancellationToken: cancellationToken);

		if (response.Count == 0)
		{
			return null;
		}

		return JsonSerializer.Deserialize<WorkloadInfo>(response.Kvs.Single().Value.ToStringUtf8());
	}

	public async Task UpdateWorkloadAsync(string identity, WorkloadInfo workloadInfo, TimeSpan lifetime, CancellationToken cancellationToken)
	{
		var lease = await _leaseClient.LeaseGrantAsync(
			new LeaseGrantRequest { TTL = (long)lifetime.TotalSeconds },
			cancellationToken: cancellationToken);

		await _kvClient.PutAsync(
			new PutRequest
			{
				Key = ByteString.CopyFromUtf8(_serviceId + identity + "WL"),
				Value = ByteString.CopyFromUtf8(JsonSerializer.Serialize(workloadInfo)),
				Lease = lease.ID
			},
			cancellationToken: cancellationToken);
	}
}
