using System.Diagnostics;
using System.Text;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvccpb;

namespace EtcdEventsCollector;

internal class Collector : IHostedService
{
	private readonly string _serviceId;
	private readonly ActivitySource _activitySource;
	private readonly ILogger<Collector> _logger;
	private readonly Watch.WatchClient _watchClient;

	private readonly Dictionary<string, Activity> _activityDictionary = new();

	public Collector(ChannelBase channel, string serviceId, ActivitySource activitySource, ILogger<Collector> logger)
	{
		_serviceId = serviceId;
		_activitySource = activitySource;
		_logger = logger;

		////_activityTags = new[] { new KeyValuePair<string, object?>("node", nodeId) };
		_watchClient = new Watch.WatchClient(channel);
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await WatchForEvents(cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogCritical(e.ToString());
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
			
	}

	private async Task WatchForEvents(CancellationToken cancellationToken)
	{
		using var watchStream = _watchClient.Watch(cancellationToken: cancellationToken);
		await watchStream.RequestStream.WriteAsync(
			new WatchRequest
			{
				CreateRequest = new WatchCreateRequest
				{
					Key = ByteString.CopyFromUtf8(_serviceId),
					RangeEnd = ByteString.CopyFromUtf8("\x00"),
				},
			},
			cancellationToken);

		await watchStream.RequestStream.CompleteAsync();

		while (await watchStream.ResponseStream.MoveNext(cancellationToken))
		{
			var events = watchStream.ResponseStream.Current.Events;

			foreach (var e in events)
			{
				HandleEvent(e.Type, e.Kv.Lease, e.Kv.Key.ToStringUtf8(), e.Kv.Value.ToStringUtf8());
			}
		}
	}


	private void HandleEvent(Event.Types.EventType type, long lease, string key, string value)
	{
		_logger.LogInformation("{Type};{Lease};{Key};{Value}", type, lease, key, value);

		var keyParts = key.Split("/");

		if (keyParts.Length > 3 && keyParts[2].Equals("Lock", StringComparison.OrdinalIgnoreCase))
		{
			var identity = keyParts[1];

			if (type == Event.Types.EventType.Put)
			{
				var activity = _activitySource.StartActivity(@"ETCD Lock");
				activity?.AddTag("node", lease);
				activity?.AddTag("identity", identity);

				if (_activityDictionary.ContainsKey(identity)) _activityDictionary.Remove(identity);
				_activityDictionary.Add(identity, activity!);
			}
			else if (type == Event.Types.EventType.Delete && _activityDictionary.ContainsKey(identity))
			{
				_activityDictionary[identity].Dispose();
				_activityDictionary.Remove(identity);
			}
		}

	}

}