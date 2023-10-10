using System.Diagnostics;
using System.Security.Cryptography;
using ExperimentalApplication.Payloads;
using ExperimentalApplication.Workers;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter.InfluxLineProtocolFile.OpenTelemetry.Exporter.InfluxLineProtocolFile;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Distributed.Prioritization.Registration;
using RecurrentWorkerService.Distributed.Registration;

using OpenTelemetry.Trace;
using RecurrentWorkerService.Workers.Models;

var etcdHostsEnv = Environment.GetEnvironmentVariable("EXPERIMENT_ETCD_HOSTS") ?? "localhost:23791;localhost:23792;localhost:23793";
var etcdHostsString = etcdHostsEnv;

var etcdBalancerAddresses = etcdHostsString.Split(";")
	.Select(v => new BalancerAddress(v.Split(":").First(), int.Parse(v.Split(":").Last())))
	.ToArray();


Console.WriteLine("Etcd hosts:");
etcdBalancerAddresses.ToList().ForEach(x => Console.WriteLine($"{x.EndPoint.Host}:{x.EndPoint.Port}"));

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


var nodeId = Math.Abs(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(64)));

var source = new ActivitySource(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name!);
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
	.AddSource(
		source.Name,
		"RecurrentWorkerService.Distributed",
		"RecurrentWorkerService.Distributed.EtcdPersistence",
		"RecurrentWorkerService.Distributed.Prioritization")
	// .AddConsoleExporter()
	.AddInfluxLineProtocolFileExporter($"/influx/App-{DateTimeOffset.Now:yyyy-MM-ddTHH.mm.ss.fffffff}-{nodeId}.influx")
	// .AddOtlpExporter(opt =>
	// {
	// 	opt.Endpoint = new Uri("http://telegraf:4317");
	// 	opt.Protocol = OtlpExportProtocol.Grpc;
	// 	opt.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
	// 	{
	// 		MaxExportBatchSize = 10000,
	// 		ScheduledDelayMilliseconds = 2000,
	// 		MaxQueueSize = 1_000_000
	// 	};
	// })
	.Build();

await Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddSingleton(source);

		services.AddDistributedWorkers(
			"LocalWorkerService",
			nodeId,
			w =>
			{
				w.AddDistributedCronWorker(
					"Cron-Immediate",
					c => new CronWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Immediate"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Fast",
					c => new CronWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Fast"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Slow",
					c => new CronWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Slow"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedCronWorker(
					"Cron-Problem",
					c => new CronWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Cron-Problem"),
						c.GetRequiredService<ILogger<CronWorker>>()),
					s => s
						.SetCronExpression("* * * * *")
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));



				w.AddDistributedRecurrentWorker(
					"Recurrent-Immediate",
					c => new RecurrentWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Immediate"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromTicks(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Fast",
					c => new RecurrentWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Fast"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Slow",
					c => new RecurrentWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Slow"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));

				w.AddDistributedRecurrentWorker(
					"Recurrent-Problem",
					c => new RecurrentWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Recurrent-Problem"),
						c.GetRequiredService<ILogger<RecurrentWorker>>()),
					s => s
						.SetPeriod(TimeSpan.FromSeconds(1))
						.SetRetryOnFailDelay(TimeSpan.FromSeconds(1)));




				w.AddDistributedWorkloadWorker(
					"Workload-Immediate",
					c => new WorkloadWorker(
						new ImmediatePayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Immediate"),
						nameof(ImmediatePayload),
						c.GetRequiredService<ActivitySource>(),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromTicks(1), TimeSpan.FromTicks(1))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Fast",
					c => new WorkloadWorker(
						new FastPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Fast"),
						nameof(FastPayload),
						c.GetRequiredService<ActivitySource>(),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Slow",
					c => new WorkloadWorker(
						new SlowPayload(c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Slow"),
						nameof(SlowPayload),
						c.GetRequiredService<ActivitySource>(),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));

				w.AddDistributedWorkloadWorker(
					"Workload-Problem",
					c => new WorkloadWorker(
						new ProblemPayload(10, c.GetRequiredService<ActivitySource>(), nodeId, "Workload-Problem"),
						nameof(ProblemPayload),
						c.GetRequiredService<ActivitySource>(),
						c.GetRequiredService<ILogger<WorkloadWorker>>()),
					s => s
						.SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))
						.SetStrategies(c => c
							.Add(Workload.Zero, TimeSpan.FromSeconds(1))
							.Subtract(Workload.FromPercent(50), TimeSpan.FromSeconds(1))));
			},
			x => x.SetHeartbeatExpirationTimeout(TimeSpan.FromSeconds(512)))
			.AddEtcdPersistence(channel)
			.AddPrioritization();
	})
	.Build()
	.RunAsync()
	.ConfigureAwait(false);
