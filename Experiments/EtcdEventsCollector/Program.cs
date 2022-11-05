using System.Diagnostics;
using System.Net;
using EtcdEventsCollector;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

HttpClient.DefaultProxy = new WebProxy();


var env = Environment.GetEnvironmentVariable("EXPERIMENT_ENV") ?? "local";
var etcdHostsKeyUrl = $"https://experiments-config.teymur.workers.dev?key={env}_etcd_hosts";
var otlpHostKeyUrl = $"https://experiments-config.teymur.workers.dev?key={env}_otlp_uri";

var etcdHostsEnv = Environment.GetEnvironmentVariable("EXPERIMENT_ETCD_HOSTS");
var etcdHostsString = etcdHostsEnv == null
	? await new HttpClient().GetAsync(etcdHostsKeyUrl).Result.EnsureSuccessStatusCode().Content.ReadAsStringAsync()
	: etcdHostsEnv;

var etcdBalancerAddresses = etcdHostsString.Split(";")
	.Select(v => new BalancerAddress(v.Split(":").First(), int.Parse(v.Split(":").Last())))
	.ToArray();



Console.WriteLine("Etcd hosts:");
etcdBalancerAddresses.ToList().ForEach(x => Console.WriteLine($"{x.EndPoint.Host}:{x.EndPoint.Port}"));

var otlpAddress = await new HttpClient().GetAsync(otlpHostKeyUrl).Result.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

Console.WriteLine("Ptlp host");
Console.WriteLine(otlpAddress);

var factory = new StaticResolverFactory(addr => etcdBalancerAddresses);
var channel = GrpcChannel.ForAddress(
	"static://",
	new GrpcChannelOptions
	{
		Credentials = ChannelCredentials.Insecure,
		ServiceProvider = new ServiceCollection().AddSingleton<ResolverFactory>(factory).BuildServiceProvider(),
		ServiceConfig = new ServiceConfig
		{
			MethodConfigs =
			{
				new MethodConfig
				{
					Names = { MethodName.Default },
					RetryPolicy = new RetryPolicy
					{
						MaxAttempts = 5,
						InitialBackoff = TimeSpan.FromSeconds(1),
						MaxBackoff = TimeSpan.FromSeconds(5),
						BackoffMultiplier = 1.5,
						RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable }
					}
				}
			}
		}
	});


var source = new ActivitySource(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name!);
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
	.AddSource(source.Name)
	.AddConsoleExporter()
	.AddOtlpExporter(opt =>
	{
		opt.Endpoint = new Uri(otlpAddress);
		opt.Protocol = OtlpExportProtocol.Grpc;
		opt.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
		{
			MaxExportBatchSize = 10000,
			ScheduledDelayMilliseconds = 2000,
			MaxQueueSize = 1_000_000
		};
	})
	.Build();


await Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService(s =>
			new Collector(
				channel,
				"LocalWorkerService",
				source,
				s.GetRequiredService<ILogger<Collector>>()));
	})
	.Build()
	.RunAsync();
