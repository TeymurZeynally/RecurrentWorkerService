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

var factory = new StaticResolverFactory(addr => new[]
{
	new BalancerAddress("localhost", 23791),
	new BalancerAddress("localhost", 23792),
	new BalancerAddress("localhost", 23793),
});

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
		opt.Endpoint = new Uri("http://localhost:4317");
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
